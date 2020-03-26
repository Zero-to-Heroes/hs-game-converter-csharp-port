using HearthstoneReplays.Parser;
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
            return false;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                && node.Type == typeof(Action)
                && (node.Object as Action).Type == (int)BlockType.TRIGGER
                && (node.Object as Action).EffectIndex == 4; 
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Action;
            var entity = GameState.CurrentEntities[action.Entity];
            if (entity.CardId != "TB_BaconShop_8P_PlayerE")
            {
                return null;
            }

            var attackAction = action.Data
                .Where(data => data.GetType() == typeof(Action))
                .Select(data => data as Action)
                .Where(act => act.Type == (int)BlockType.ATTACK)
                .FirstOrDefault();


            if (attackAction == null)
            {
                return new List<GameEventProvider> { GameEventProvider.Create(
                    action.TimeStamp,
                    () => new GameEvent
                    {
                        Type = "BATTLEGROUNDS_BATTLE_RESULT",
                        Value = new
                        {
                            Result = "tied"
                        }
                    },
                    true,
                    node.CreationLogLine)
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
            var opponent = GameState.CurrentEntities[opponentEntityId].CardId;
            var damage = damageTag != null ? damageTag.Value : 0;

            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                () => new GameEvent
                {
                    Type = "BATTLEGROUNDS_BATTLE_RESULT",
                    Value = new
                    {
                        Opponent = opponent,
                        Result = result,
                        Damage = damage,
                    }
                },
                true,
                node.CreationLogLine)
            };
        }
    }
}
