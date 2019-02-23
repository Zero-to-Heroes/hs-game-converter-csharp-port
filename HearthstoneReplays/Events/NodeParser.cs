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
        private List<ActionParser> parsers;
        private List<GameEventProvider> eventQueue;
        private Timer timer;
        private ParserState ParserState;

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

        public async void NewNode(Node node)
        {
            foreach (ActionParser parser in parsers)
            {
                if (parser.NeedMetaData())
                {
                    // Wait until we have all the necessary data
                    while (ParserState.CurrentGame.FormatType == 0 || ParserState.CurrentGame.GameType == 0 || ParserState.LocalPlayer == null)
                    {
                        await Task.Delay(100);
                    }
                }
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

        public async void CloseNode(Node node)
        {
            if (node == null)
            {
                return;
            }
            foreach (ActionParser parser in parsers)
            {
                if (parser.NeedMetaData())
                {
                    // Wait until we have all the necessary data
                    while (ParserState.CurrentGame.FormatType == 0 || ParserState.CurrentGame.GameType == 0 || ParserState.LocalPlayer == null)
                    {
                        await Task.Delay(100);
                    }
                }
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
            eventQueue.Add(provider);
            eventQueue = eventQueue.OrderBy(p => p.Timestamp).ToList();
        }

        private List<ActionParser> BuildActionParsers(ParserState ParserState)
        {
            return new List<ActionParser>()
            {
                new NewGameParser(),
                new CardPlayedFromHandParser(ParserState),
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
                //TODO: shrine played at start of game
            };
        }

        private void ProcessGameEventQueue(Object source, ElapsedEventArgs e)
        {
            // If both the first events has just been added, wait a bit, so that we're sure there's no 
            // other event that should be processed first
            // Warning: this means the whole event parsing works in real-time, and is not suited for 
            // post-processing of games
            while (eventQueue.Count > 0 && DateTimeOffset.UtcNow.Subtract(eventQueue.First().Timestamp).Milliseconds > 500)
            {
                GameEventProvider provider = eventQueue[0];
                eventQueue.RemoveAt(0);
                GameEventHandler.Handle(provider.GameEvent);
            }
        }
    }
}