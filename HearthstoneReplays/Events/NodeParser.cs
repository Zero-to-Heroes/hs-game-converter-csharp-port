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
        public List<GameEventProvider> eventQueue;

        private List<ActionParser> parsers;
        private Timer timer;
        private ParserState ParserState;

        private readonly Object listLock = new object();

        private bool metadataEmit;

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
            //ClearQueue();
            // Logger.Log("before reset", eventQueue.Count);
            this.ParserState = ParserState;
            this.parsers = BuildActionParsers(ParserState);
            this.metadataEmit = false;
            // Logger.Log("after reset", eventQueue.Count);
        }

        public void StartDevMode()
        {
            //lock (listLock)
            //{
            //    Logger.Log("Enqueuing start dev mode", eventQueue.Count);
            //    eventQueue.Add(new StartDevModeProvider());
            //}
        }

        public async void StopDevMode()
        {
            //Logger.Log("Will enqueue stop dev mode", "");
            //await Task.Delay(5000);
            //lock (listLock)
            //{
            //    Logger.Log("Enqueuing stop dev mode", eventQueue.Count);
            //    eventQueue.Add(new StopDevModeProvider());
            //}
        }

        public void NewNode(Node node)
        {
            try
            {
                if (node == null)
                {
                    return;
                }
                //Logger.Log("Receiving new node", node.CreationLogLine);
                foreach (ActionParser parser in parsers)
                {
                    if (parser.AppliesOnNewNode(node))
                    {
                        List<GameEventProvider> providers = parser.CreateGameEventProviderFromNew(node);
                        if (providers != null)
                        {
                            EnqueueGameEvent(providers.Where(provider => provider != null).ToList());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Coulnt not apply parsers to new node, ignoring node and moving on " + e.Message, e.StackTrace);
            }
        }

        public void CloseNode(Node node)
        {
            if (node == null)
            {
                return;
            }
            //Logger.Log("Receiving close node", node.CreationLogLine);
            foreach (ActionParser parser in parsers)
            {
                if (!node.Closed && parser.AppliesOnCloseNode(node))
                {
                    List<GameEventProvider> providers = parser.CreateGameEventProviderFromClose(node);
                    if (providers != null && providers.Count > 0)
                    {
                        EnqueueGameEvent(providers.Where(provider => provider != null).ToList());
                    }
                }
            }
            // Make sure we don't process the same node twice
            node.Closed = true;
        }

        //public void AddEventAtStart(List<GameEventProvider> providers)
        //{
        //    lock (listLock)
        //    {
        //        eventQueue.InsertRange(0, providers);
        //    }
        //}

        public void EnqueueGameEvent(List<GameEventProvider> providers)
        {
            //Logger.Log("[csharp] Enqueueing game event", providers != null ? providers[0].CreationLogLine : null);
            lock (listLock)
            {
                //Logger.Log("Acquierd list lock in queneGameEvent", "");
                // Remove outstanding events
                if (providers.Any(provider => provider.CreationLogLine.Contains("CREATE_GAME")) && eventQueue.Count > 0)
                {
                    Logger.Log("Purging queue of outstanding events", eventQueue.Count);
                    ClearQueue();
                }

                var priorityProviders = providers
                    .Where(p => p.EventName == "MATCH_METADATA" || p.EventName == "NEW_GAME")
                    .ToList();
                if (priorityProviders.Count > 0)
                {
                    priorityProviders
                        .OrderBy(p => p.Timestamp)
                        .ToList()
                        .ForEach(provider => ProcessGameEvent(provider));
                    metadataEmit = true;
                    return;
                }

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
                    //Logger.Log("Before culling dupes", eventQueue.Count);
                    eventQueue = eventQueue
                        .Where(queued => queued != null)
                        .Where((queued) => !shouldUnqueuePredicates.Any((predicate) => predicate(queued)))
                        .ToList();
                    //Logger.Log("After culling dupes", eventQueue.Count);
                }
                eventQueue.AddRange(providers);
                eventQueue = eventQueue.OrderBy(p => p.Timestamp).ToList();
            }
        }

        public void ClearQueue()
        {
            lock (listLock)
            {
                //Logger.Log("Acquierd list lock in clearqueue", "");
                // We process all pending events. It can happen (typically with the Hearthstone spell) that 
                // not all events receive their animation log, so we do it just to be sure there aren't any 
                // left overs
                eventQueue.ForEach(provider => ProcessGameEvent(provider));
                eventQueue.Clear();
            }
        }

        private async void ProcessGameEventQueue(Object source, ElapsedEventArgs e)
        {
            while (IsEventToProcess())
            {
                //Logger.Log("There is an event to process", eventQueue.Count == 0 ? "nothing" : eventQueue[0].CreationLogLine);
                try
                {
                    GameEventProvider provider;
                    lock (listLock)
                    {
                        //Logger.Log("Acquierd list lock in processgameevent", "");
                        if (eventQueue.Count == 0)
                        {
                            //Logger.Log("Queue empty", "");
                            return;
                        }
                        provider = eventQueue[0];
                        eventQueue.RemoveAt(0);
                    }
                    while (provider.NeedMetaData && (!this.metadataEmit 
                            || ParserState.CurrentGame.FormatType == -1 
                            || ParserState.CurrentGame.GameType == -1 
                            || ParserState.LocalPlayer == null))
                    {
                        // Wait until we have all the necessary data
                        while (ParserState.CurrentGame.FormatType == -1 || ParserState.CurrentGame.GameType == -1 || ParserState.LocalPlayer == null)
                        {
                            await Task.Delay(100);
                        }
                    }
                    lock (listLock)
                    {
                        //Logger.Log("Acquierd list lock in processgameevent 2", "");
                        ProcessGameEvent(provider);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Exception while parsing event queue " + ex.Message, ex.StackTrace);
                    return;
                }
            }
        }

        private void ProcessGameEvent(GameEventProvider provider)
        {
            if (provider.SupplyGameEvent == null && provider.GameEvent == null)
            {
                Logger.Log("No game event", provider.CreationLogLine);
                return;
            }
            var gameEvent = provider.GameEvent != null ? provider.GameEvent : provider.SupplyGameEvent();
            //if (provider.CreationLogLine.Contains("3651")) // == "TAG_CHANGE Entity=[entityName=Voidwalker id=3651 zone=PLAY zonePos=3 cardId=CS2_065 player=13] tag=ZONE value=REMOVEDFROMGAMETAG_CHANGE Entity=[entityName=Voidwalker id=3651 zone=PLAY zonePos=3 cardId=CS2_065 player=13] tag=ZONE value=REMOVEDFROMGAME")
            //{
            //    Logger.Log("Handling debug provider", provider.CreationLogLine);
            //}
            if (provider.debug)
            {
                Logger.Log("[csharp] should provide event? " + (gameEvent != null), provider.CreationLogLine + " // " + provider.AnimationReady);
                Logger.Log(
                    "[csharp] animation ready stuff",
                    string.Join("\\n", eventQueue.Where(p => p.AnimationReady).Select(p => p.CreationLogLine)));
            }
            // This can happen because there are some conditions that are only resolved when we 
            // have the full meta data, like dungeon run step
            if (gameEvent != null)
            {
                if (provider.debug)
                {
                    Logger.Log("[csharp] Handling event", gameEvent.Type);
                }
                GameEventHandler.Handle(gameEvent);
            }
            else
            {
                //Logger.Log("[csharp] Game event is null, so doing nothing", provider.CreationLogLine);
            }
        }

        private bool IsEventToProcess()
        {
            try
            {
                var isEvent = false;

                lock (listLock)
                {
                    //Logger.Log("Acquierd list lock in iseventtoprocess", "");
                    // We leave some time so that events parsed later can be processed sooner (typiecally the case 
                    // for end-of-block events vs start-of-block events, like tag changes)
                    isEvent = eventQueue.Count > 0;
                        //&& (
                        //    DevMode
                        //    || eventQueue.First() is StartDevModeProvider
                        //    || DateTime.Now.Subtract(eventQueue.First().Timestamp).TotalMilliseconds > 500);
                    //if (eventQueue.Count > 0)
                    //{
                    //    //    Logger.Log("Is event to process? " + isEvent, DateTime.Now + " // "
                    //    //        + eventQueue.First().Timestamp
                    //    //        + " // " + DateTime.Now.Subtract(eventQueue.First().Timestamp).TotalMilliseconds);
                    //    Logger.Log("Is event to process? " + isEvent, eventQueue[0].CreationLogLine);
                    //}
                }
                //if (eventQueue.Count > 0 && !isEvent)
                //{
                //    Logger.Log("[csharp] too soon to process events", eventQueue.First().CreationLogLine);
                //    Logger.Log(DateTime.Now.Subtract(eventQueue.First().Timestamp).TotalMilliseconds, "");
                //    Logger.Log(DateTime.Now, eventQueue.First().Timestamp);
                //}
                return isEvent;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.StackTrace, "" + eventQueue.Count);
                Logger.Log("Exception while trying to determine event to process", ex.Message);
                return false;
            }
        }

        private List<ActionParser> BuildActionParsers(ParserState ParserState)
        {
            return new List<ActionParser>()
            {
                new NewGameParser(),
                new WinnerParser(ParserState),
                new GameEndParser(ParserState),
                new TurnStartParser(ParserState),
                new FirstPlayerParser(ParserState),
                new MainStepReadyParser(ParserState),
                new CardPlayedFromHandParser(ParserState),
                new SecretPlayedFromHandParser(ParserState),
                new MulliganInputParser(ParserState),
                new MulliganDoneParser(ParserState),
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
                new CardRemovedFromBoardParser(ParserState),
                new MinionOnBoardAttackUpdatedParser(ParserState),
                new RecruitParser(ParserState),
                new MinionBackOnBoardParser(ParserState),
                new CardRevealedParser(ParserState),
                new InitialCardInDeckParser(ParserState),
                new MinionSummonedParser(ParserState),
                new FatigueParser(ParserState),
                new DamageParser(ParserState),
                new HealingParser(ParserState),
                new BurnedCardParser(ParserState),
                new MinionDiedParser(ParserState),
                new SecretPlayedFromDeckParser(ParserState),
                new SecretCreatedInGameParser(ParserState),
                new SecretDestroyedParser(ParserState),
                new ArmorChangeParser(ParserState),
                new CardStolenParser(ParserState),
                new SecretTriggeredParser(ParserState),
                new DeathrattleTriggeredParser(ParserState),
                new HealthDefChangeParser(ParserState),
                new ChangeCardCreatorParser(ParserState),
                new LocalPlayerLeaderboardPlaceChangedParser(ParserState),
                new HeroPowerChangedParser(ParserState),
                new WeaponEquippedParser(ParserState),
                new WeaponDestroyedParser(ParserState),
                new BattlegroundsPlayerBoardParser(ParserState),
                new BattlegroundsPlayerTechLevelUpdatedParser(ParserState),
                new BattlegroundsPlayerLeaderboardPlaceUpdatedParser(ParserState),
                new BattlegroundsHeroSelectionParser(ParserState),
                new BattlegroundsNextOpponnentParser(ParserState),
                new BattlegroundsTriplesCountUpdatedParser(ParserState),
                //new BattlegroundsStartOfCombatParser(ParserState),
                new BattlegroundsOpponentRevealedParser(ParserState),
                new BattlegroundsHeroSelectedParser(ParserState),
                new BattlegroundsBattleOverParser(ParserState),
                new DecklistUpdateParser(ParserState),
                new GameRunningParser(ParserState),
                new AttackParser(ParserState),
                new NumCardsPlayedThisTurnParser(ParserState),
                new HeroPowerUsedParser(ParserState),
                new GalakrondInvokedParser(ParserState),
                new CardBuffedInHandParser(ParserState),
                new MinionGoDormantParser(ParserState),
                new BlockEndParser(ParserState),
                new JadeGolemParser(ParserState),
                new CthunParser(ParserState),
            };
        }
    }
}