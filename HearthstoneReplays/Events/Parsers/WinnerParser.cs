using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class WinnerParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public WinnerParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.PLAYSTATE;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            if (tagChange.Value == (int)PlayState.WON)
            {
                var winner = (PlayerEntity)ParserState.GetEntity(tagChange.Entity);
                var gameStateReport = GameState.BuildGameStateReport();
                //Logger.Log("Creating event provider for WinnerParser", node.CreationLogLine);
                return new List<GameEventProvider> { GameEventProvider.Create(
                       tagChange.TimeStamp,
                       () => {
                            //Logger.Log("Providing game event for WinnerParser", node.CreationLogLine);
                            return new GameEvent
                            {
                                Type = "WINNER",
                                Value = new
                                {
                                    Winner = winner,
                                    LocalPlayer = ParserState.LocalPlayer,
                                    OpponentPlayer = ParserState.OpponentPlayer,
                                    GameStateReport = gameStateReport,
                                }
                            };
                       },
                       true,
                       node.CreationLogLine,
                       true) };
            }
            else if (tagChange.Value == (int)PlayState.TIED)
            {
                return new List<GameEventProvider> { GameEventProvider.Create(
                       tagChange.TimeStamp,
                       () => new GameEvent
                       {
                           Type = "TIE"
                       },
                       true,
                       node.CreationLogLine) };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
