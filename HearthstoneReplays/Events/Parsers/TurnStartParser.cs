using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class TurnStartParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public TurnStartParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.TURN;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            GameState.CurrentTurn = (int)tagChange.Value;
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            var result = new List<GameEventProvider>();
            result.Add(GameEventProvider.Create(
                   tagChange.TimeStamp,
                   "TURN_START",
                   () => new GameEvent
                   {
                       Type = "TURN_START",
                       Value = new
                       {
                           Turn = (int)tagChange.Value,
                           GameState = gameState,
                           LocalPlayer = ParserState.LocalPlayer,
                           OpponentPlayer = ParserState.OpponentPlayer,
                       }
                   },
                   false,
                   node.CreationLogLine));
            // This seems the most reliable way to have the combat_start event as soon as possible
            if (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS && tagChange.Value % 2 == 0) 
            {
                result.Add(GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "BATTLEGROUNDS_COMBAT_START",
                    () => new GameEvent
                    {
                        Type = "BATTLEGROUNDS_COMBAT_START"
                    },
                    false,
                    node.CreationLogLine));
            }
            return result;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
