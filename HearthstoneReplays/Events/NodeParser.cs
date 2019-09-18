#region
using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Events.Parsers;
#endregion

namespace HearthstoneReplays.Events
{
	public class NodeParser
    {
        public static bool DevMode = false;

        private List<ActionParser> parsers;
        private List<GameEventProvider> eventQueue;
        private Timer timer;
        private ParserState ParserState;

        private readonly Object listLock = new object();

        public NodeParser()
        {
            eventQueue = new List<GameEventProvider>();
            // Check the queue every 100 ms
            timer = new Timer(100);
            timer.Elapsed += ProcessGameEventQueue;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public void Reset(ParserState ParserState)
        {
            this.ParserState = ParserState;
            parsers = BuildActionParsers(ParserState);
        }

        public void NewNode(Node node)
        {
            if (node == null)
            {
                return;
            }
            foreach (ActionParser parser in parsers)
            {
                if (parser.AppliesOnNewNode(node))
                {
                    List<GameEventProvider> providers = parser.CreateGameEventProviderFromNew(node);
                    if (providers != null)
                    {
                        EnqueueGameEvent(providers);
                    }
                }
            }
        }

        public void CloseNode(Node node)
        {
            if (node == null)
            {
                return;
            }
            foreach (ActionParser parser in parsers)
            {
                if (!node.Closed && parser.AppliesOnCloseNode(node))
                {
                    List<GameEventProvider> providers = parser.CreateGameEventProviderFromClose(node);
                    if (providers != null)
                    {
                        EnqueueGameEvent(providers);
                    }
                }
            }
            // Make sure we don't process the same node twice
            node.Closed = true;
        }

        public void EnqueueGameEvent(List<GameEventProvider> providers)
        {
            lock(listLock)
            {
                var shouldUnqueuePredicates = providers
                    .Select(provider => provider.isDuplicatePredicate)
                    .ToList();
                // Remove duplicate events
                // As we process the queue when the animation is ready, we should not have a race condition 
                // here, but it's still risky (vs preventing the insertion if a future event is a duplicate, but 
                // which requires a lot of reengineering of the loop)
                if (eventQueue != null 
                    && eventQueue.Count > 0 
                    && shouldUnqueuePredicates != null
                    && shouldUnqueuePredicates.Count > 0)
                {
                    eventQueue = eventQueue
                        .Where(queued => queued != null)
                        .Where((queued) => !shouldUnqueuePredicates.Any((predicate) => predicate(queued)))
                        .ToList();
                }
                eventQueue.AddRange(providers);
                eventQueue = eventQueue.OrderBy(p => p.Timestamp).ToList();
            }
        }

        public void ReceiveAnimationLog(string data)
        {
            lock (listLock)
            { 
                if (eventQueue.Count > 0)
                {
                    foreach (GameEventProvider provider in eventQueue)
                    {
                        provider.ReceiveAnimationLog(data, ParserState);
                    }
                }
            }
        }

        private List<ActionParser> BuildActionParsers(ParserState ParserState)
        {
            return new List<ActionParser>()
            {
                new NewGameParser(),
                new CardPlayedFromHandParser(ParserState),
                new SecretPlayedFromHandParser(ParserState),
                new WinnerParser(ParserState),
                new GameEndParser(ParserState),
                new MulliganInputParser(ParserState),
                new MulliganDoneParser(ParserState),
                new TurnStartParser(ParserState),
                new RumbleRunStepParser(ParserState),
                new DungeonRunStepParser(ParserState),
                new MonsterRunStepParser(ParserState),
                new PassiveBuffParser(ParserState),
                new CardPresentOnGameStartParser(ParserState),
                new CardDrawFromDeckParser(ParserState),
                new ReceiveCardInHandParser(ParserState),
                new CardBackToDeckParser(ParserState),
                new DiscardedCardParser(ParserState),
                new CardRemovedFromDeckParser(ParserState),
                new CreateCardInDeckParser(ParserState),
                new EndOfEchoInHandParser(ParserState),
                new CardChangedParser(ParserState),
                new CardRemovedFromHandParser(ParserState),
                new MinionOnBoardAttackUpdatedParser(ParserState),
                new RecruitParser(ParserState),
                new MinionSummonedParser(ParserState),
                new FatigueParser(ParserState),
                new DamageParser(ParserState),
                new HealingParser(ParserState),
                new BurnedCardParser(ParserState),
                new MinionDiedParser(ParserState),
                new SecretPlayedFromDeckParser(ParserState),
                new FirstPlayerParser(ParserState),
                new MainStepReadyParser(ParserState),
                new ArmorChangeParser(ParserState),
                new CardStolenParser(ParserState),
            };
        }

        private async void ProcessGameEventQueue(Object source, ElapsedEventArgs e)
        {
            // If both the first events has just been added, wait a bit, so that we're sure there's no 
            // other event that should be processed first
            // Warning: this means the whole event parsing works in real-time, and is not suited for 
            // post-processing of games
            while (IsEventToProcess())
            {
                try
                {
                    GameEventProvider provider;
                    lock (listLock)
                    {
                        if (eventQueue.Count == 0)
                        {
                            return;
                        }
                        if (!eventQueue.Any(p => p.AnimationReady) 
                            && !ParserState.Ended
                            // Safeguard - Don't wait too long for the animation in case we never receive it
                            && DateTimeOffset.UtcNow.Subtract(eventQueue.First().Timestamp).TotalMilliseconds < 5000)
                        {
                            return;
                        }
                        provider = eventQueue[0];
                        eventQueue.RemoveAt(0);
                    }
                    if (provider.NeedMetaData)
                    {
                        // Wait until we have all the necessary data
                        while (ParserState.CurrentGame.FormatType == 0 || ParserState.CurrentGame.GameType == 0 || ParserState.LocalPlayer == null)
                        {
                            await Task.Delay(100);
                        }
                    }
                    lock (listLock)
                    {
                        var gameEvent = provider.GameEvent != null ? provider.GameEvent : provider.SupplyGameEvent();
                        // This can happen because there are some conditions that are only resolved when we 
                        // have the full meta data, like dungeon run step
                        if (gameEvent != null)
                        {
                            GameEventHandler.Handle(gameEvent);
                        }
                        else
                        {
                            Logger.Log("Game event is null, so doing nothing", provider.CreationLogLine);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Exception while parsing event queue", ex.Message);
                    Logger.Log(ex.StackTrace, "" + eventQueue.Count);
                    return;
                }
            }
        }

        private bool IsEventToProcess()
        {
            try
            {
                // We leave some time so that events parsed later can be processed sooner (typiecally the case 
                // for end-of-block events vs start-of-block events, like tag changes)
                return eventQueue.Count > 0
                        && (DevMode || DateTimeOffset.UtcNow.Subtract(eventQueue.First().Timestamp).TotalMilliseconds > 500);
            }
            catch (Exception ex)
            {
                Logger.Log("Exception while trying to determine event to process", ex.Message);
                Logger.Log(ex.StackTrace, "" + eventQueue.Count);
                return false;
            }
        }
    }
}