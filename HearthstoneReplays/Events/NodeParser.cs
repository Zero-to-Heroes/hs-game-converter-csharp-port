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
                    GameEventProvider provider = parser.CreateGameEventProviderFromNew(node);
                    if (provider != null)
                    {
                        EnqueueGameEvent(provider);
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
                if (parser.AppliesOnCloseNode(node))
                {
                    GameEventProvider provider = parser.CreateGameEventProviderFromClose(node);
                    if (provider != null)
                    {
                        EnqueueGameEvent(provider);
                    }
                }
            }
        }

        public void EnqueueGameEvent(GameEventProvider provider)
        {
            lock(listLock)
            {
                eventQueue.Add(provider);
                eventQueue = eventQueue.OrderBy(p => p.Timestamp).ToList();
            }
        }

        public void ReceiveAnimationLog(string data)
        {
            lock (listLock)
            { 
                foreach (GameEventProvider provider in eventQueue)
                {
                    provider.ReceiveAnimationLog(data, ParserState);
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
                // TODO: discover
                new DiscardedCardParser(ParserState),
                new CardRemovedFromDeckParser(ParserState),
                new CreateCardInDeckParser(ParserState),
            };
        }

        private async void ProcessGameEventQueue(Object source, ElapsedEventArgs e)
        {
            // If both the first events has just been added, wait a bit, so that we're sure there's no 
            // other event that should be processed first
            // Warning: this means the whole event parsing works in real-time, and is not suited for 
            // post-processing of games
            while (eventQueue.Count > 0 
                && (DevMode || DateTimeOffset.UtcNow.Subtract(eventQueue.First().Timestamp).Milliseconds > 500))
            {
                lock (listLock)
                {
                    if (!eventQueue.Any(p => p.AnimationReady))
                    {
                        return;
                    }
                }
                GameEventProvider provider = eventQueue[0];
                eventQueue.RemoveAt(0);
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
                    var gameEvent = provider.SupplyGameEvent();
                    // This can happen because there are some conditions that are only resolved when we 
                    // have the full meta data, like dungeon run step
                    if (gameEvent != null)
                    {
                        GameEventHandler.Handle(gameEvent);
                    }
                }
            }
        }
    }
}