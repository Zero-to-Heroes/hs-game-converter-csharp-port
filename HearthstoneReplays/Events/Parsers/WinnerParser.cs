using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

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

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            if (tagChange.Value == (int)PlayState.WON)
            {
                var winner = (PlayerEntity)ParserState.GetEntity(tagChange.Entity);
                return GameEventProvider.Create(
                       tagChange.TimeStamp,
                       () => new GameEvent
                       {
                           Type = "WINNER",
                           Value = new
                           {
                               Winner = winner,
                               LocalPlayer = ParserState.LocalPlayer,
                               OpponentPlayer = ParserState.OpponentPlayer
                           }
                       },
                       true,
                       node.CreationLogLine);
            }
            else if (tagChange.Value == (int)PlayState.TIED)
            {
                return GameEventProvider.Create(
                       tagChange.TimeStamp,
                       () => new GameEvent
                       {
                           Type = "TIE"
                       },
                       true,
                       node.CreationLogLine);
            }
            return null;
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
