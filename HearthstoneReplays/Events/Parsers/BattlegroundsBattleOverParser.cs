﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using System;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsBattleOverParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsBattleOverParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            // The tag change is only used as a fallback mechanism in case no battle result was sent
            // This can happen in case of draws sometimes
            return (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                    || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                && !GameState.BattleResultSent
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.BOARD_VISUAL_STATE
                && (node.Object as TagChange).Value == 1;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                    || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                && node.Type == typeof(Action)
                && (node.Object as Action).Type == (int)BlockType.TRIGGER
                && (node.Object as Action).EffectIndex == 6; 
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            GameState.BattleResultSent = true;
            var tagChange = node.Object as TagChange;
            string opponentCardId = GameState.BgsCurrentBattleOpponent;
            return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                     "BATTLEGROUNDS_BATTLE_RESULT",
                    () => new GameEvent
                    {
                        Type = "BATTLEGROUNDS_BATTLE_RESULT",
                        Value = new
                        {
                            Opponent = opponentCardId,
                            Result = "tied"
                        }
                    },
                    true,
                    node)
                };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Action;
            var entity = GameState.CurrentEntities[action.Entity];
            if (entity.CardId != "TB_BaconShop_8P_PlayerE")
            {
                return null;
            }

            var isAttackNode = action.Data
                .Where(data => data.GetType() == typeof(TagChange))
                .Select(data => data as TagChange)
                .Where(tag => (tag.Name == (int)GameTag.BACON_HIGHLIGHT_ATTACKING_MINION_DURING_COMBAT && tag.Value == 0))
                .Count() > 0;
            if (!isAttackNode)
            {
                return null;
            }

            GameState.BattleResultSent = true;
            var attackAction = action.Data
                .Where(data => data.GetType() == typeof(Action))
                .Select(data => data as Action)
                .Where(act => act.Type == (int)BlockType.ATTACK)
                .FirstOrDefault();

            if (attackAction == null)
            {
                var opponentPlayerId = ParserState.OpponentPlayer.PlayerId;
                var opponentHero = GameState.CurrentEntities.Values
                    .Where(data => data.GetTag(GameTag.CONTROLLER) == opponentPlayerId)
                    .Where(data => data.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                    .Where(data => data.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .FirstOrDefault();
                var cardId = opponentHero?.CardId;
                if (cardId == NonCollectible.Neutral.KelthuzadTavernBrawl2)
                {
                    // Find the nexwt_opponent_id
                    var player = GameState.CurrentEntities[ParserState.LocalPlayer.Id];
                    opponentPlayerId = player.GetTag(GameTag.NEXT_OPPONENT_PLAYER_ID);
                    opponentHero = GameState.CurrentEntities.Values
                        .Where(data => data.GetTag(GameTag.PLAYER_ID) == opponentPlayerId)
                        .FirstOrDefault();
                    cardId = opponentHero?.CardId;
                }
                return new List<GameEventProvider> { GameEventProvider.Create(
                    action.TimeStamp,
                     "BATTLEGROUNDS_BATTLE_RESULT",
                    () => new GameEvent
                    {
                        Type = "BATTLEGROUNDS_BATTLE_RESULT",
                        Value = new
                        {
                            Opponent = cardId,
                            Result = "tied"
                        }
                    },
                    true,
                    node)
                };
            }

            var winner = GameState.CurrentEntities[attackAction.Entity];
            var result = winner.GetTag(GameTag.CONTROLLER) == ParserState.LocalPlayer.PlayerId ? "won" : "lost";
            var damageTag = attackAction.Data
                .Where(data => data.GetType() == typeof(TagChange))
                .Select(data => data as TagChange)
                .Where(tag => tag.Name == (int)GameTag.PREDAMAGE)
                .FirstOrDefault();
            var attackerEntityId = attackAction.Data
                .Where(data => data.GetType() == typeof(TagChange))
                .Select(data => data as TagChange)
                .Where(tag => tag.Name == (int)GameTag.ATTACKING && tag.Value == 1)
                .FirstOrDefault()
                .Entity;
            var defenderEntityId = attackAction.Data
                .Where(data => data.GetType() == typeof(TagChange))
                .Select(data => data as TagChange)
                .Where(tag => tag.Name == (int)GameTag.DEFENDING && tag.Value == 1)
                .FirstOrDefault()
                .Entity;
            var opponentEntityId = GameState.CurrentEntities[attackerEntityId].GetTag(GameTag.CONTROLLER) == ParserState.LocalPlayer.PlayerId
                ? defenderEntityId
                : attackerEntityId;
            var opponentCardId = GameState.CurrentEntities[opponentEntityId].CardId;
            var damage = damageTag != null ? damageTag.Value : 0;

            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                "BATTLEGROUNDS_BATTLE_RESULT",
                () => new GameEvent
                {
                    Type = "BATTLEGROUNDS_BATTLE_RESULT",
                    Value = new
                    {
                        Opponent = opponentCardId,
                        Result = result,
                        Damage = damage,
                    }
                },
                true,
                node)
            };
        }
    }
}
