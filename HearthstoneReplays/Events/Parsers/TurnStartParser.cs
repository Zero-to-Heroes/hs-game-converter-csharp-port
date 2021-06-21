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
            return (ParserState.ReconnectionOngoing || (ParserState.Spectating && ParserState.IsBattlegrounds()))
                && node.Type == typeof(GameEntity);
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
            // FIXME?: maybe this should not be inside the event provider, but rather apply on the GameState
            GameState.OnNewTurn();
            result.Add(GameEventProvider.Create(
                   tagChange.TimeStamp,
                   "TURN_START",
                   () =>
                   {
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
                   node));
            // This seems the most reliable way to have the combat_start event as soon as possible
            if ((ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                        || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY))
            {
                if (tagChange.Value % 2 == 0)
                {
                    GameState.BattleResultSent = false;
                    result.Add(GameEventProvider.Create(
                        tagChange.TimeStamp,
                        "BATTLEGROUNDS_COMBAT_START",
                        () => new GameEvent
                        {
                            Type = "BATTLEGROUNDS_COMBAT_START"
                        },
                        false,
                        node));
                }
                else
                {
                    result.Add(GameEventProvider.Create(
                        tagChange.TimeStamp,
                        "BATTLEGROUNDS_RECRUIT_PHASE",
                        () => new GameEvent
                        {
                            Type = "BATTLEGROUNDS_RECRUIT_PHASE"
                        },
                        false,
                        node));
                }
            }
            return result;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var gameEntity = node.Object as GameEntity;
            var currentTurn = gameEntity.GetTag(GameTag.TURN);
            GameState.CurrentTurn = currentTurn;

            var result = new List<GameEventProvider>();
            result.Add(GameEventProvider.Create(
                gameEntity.TimeStamp,
                "TURN_START",
                () =>
                {
                    return new GameEvent
                    {
                        Type = "TURN_START",
                        Value = new
                        {
                            Turn = currentTurn,
                            LocalPlayer = ParserState.LocalPlayer,
                            OpponentPlayer = ParserState.OpponentPlayer,
                        }
                    };
                },
                false,
                node));
            return result;
        }
    }
}
