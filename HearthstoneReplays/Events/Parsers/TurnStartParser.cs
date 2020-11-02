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
            //GameState.StartTurn();
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            var result = new List<GameEventProvider>();
            // This event system is sometimes a mess - in some cases we want to reset the info when the event is sent
            // and in others we want to reset the game state, so as it is processed
            GameState.ClearPlagiarize();
            result.Add(GameEventProvider.Create(
                   tagChange.TimeStamp,
                   "TURN_START",
                   () =>
                   {
                       // FIXME?: maybe this should not be inside the event provider, but rather apply on the GameState
                       GameState.OnNewTurn();
                       return new GameEvent
                       {
                           Type = "TURN_START",
                           Value = new
                           {
                               Turn = (int)tagChange.Value,
                               GameState = gameState,
                               LocalPlayer = ParserState.LocalPlayer,
                               OpponentPlayer = ParserState.OpponentPlayer,
                           }
                       };
                   },
                   false,
                   node.CreationLogLine));
            // This seems the most reliable way to have the combat_start event as soon as possible
            if ((ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                        || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                    && tagChange.Value % 2 == 0)
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
