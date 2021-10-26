﻿using HearthstoneReplays.Parser;
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
            var isNormalTurnChange = !ParserState.IsMercenaries() 
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.TURN
                && GameState.GetGameEntity()?.Entity == (node.Object as TagChange).Entity;
            // While the TURN tag is present in mercenaries, it is incremented on the Innkeeper entity,
            // and the logs don't let us easily disambiguate between the AI Innkeeper and the player's
            // Innkeeper, so we rely on the turn structure tags instead
            var isMercenariesTurnChange = ParserState.IsMercenaries() 
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.STEP
                && (node.Object as TagChange).Value == (int)Step.MAIN_PRE_ACTION
                && GameState.GetGameEntity()?.Entity == (node.Object as TagChange).Entity;
            return isNormalTurnChange || isMercenariesTurnChange;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            var isGameNode = node.Type == typeof(GameEntity);
            return (ParserState.ReconnectionOngoing || ParserState.Spectating)
                && isGameNode;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var newTurnValue = tagChange.Name == (int)GameTag.TURN ? (int)tagChange.Value : GameState.CurrentTurn + 1;
            GameState.CurrentTurn = newTurnValue;
            //GameState.StartTurn();
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            var result = new List<GameEventProvider>();
            // This event system is sometimes a mess - in some cases we want to reset the info when the event is sent
            // and in others we want to reset the game state, so as it is processed
            GameState.ClearPlagiarize();
            // FIXME?: maybe this should not be inside the event provider, but rather apply on the GameState
            GameState.OnNewTurn();
            if (ParserState.IsBattlegrounds())
            {
                if (newTurnValue % 2 != 0)
                {
                    // When at the top2 stage, the event isn't sent anymore, so we send a default event
                    // when the turn starts (from what I've seen, the event is always sent before the turn
                    // starts)
                    // Do it first so that it happens before the TURN_START event
                    if (!GameState.BgsHasSentNextOpponent)
                    {
                        Logger.Log("Has not sent next opponent", "");
                        result.Add(GameEventProvider.Create(
                            tagChange.TimeStamp,
                            "BATTLEGROUNDS_NEXT_OPPONENT",
                            () => new GameEvent
                            {
                                Type = "BATTLEGROUNDS_NEXT_OPPONENT",
                                Value = new
                                {
                                    IsSameOpponent = true,
                                }
                            },
                            true,
                            node,
                            false,
                            false)
                        );
                        GameState.BgsHasSentNextOpponent = true;
                    }
                }
            }
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
                               Turn = newTurnValue,
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
                if (newTurnValue % 2 == 0)
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
                // So that we don't send the "turn start" event before the metadata (which triggers the creation of the 
                // game client-side) is processed
                ParserState.Spectating,
                node));
            return result;
        }
    }
}
