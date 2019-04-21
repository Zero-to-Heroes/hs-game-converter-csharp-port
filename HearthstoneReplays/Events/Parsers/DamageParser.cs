﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class DamageParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public DamageParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return false;
            //return node.Type == typeof(Info)
            //    && node.Parent.Type == typeof(MetaData)
            //    && (node.Parent.Object as MetaData).Meta == (int)MetaDataType.DAMAGE;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.Action)
                && HasDamageTag(node.Object as Parser.ReplayData.GameActions.Action);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
            //Node parentAction = node;
            //while (parentAction != null && parentAction.Type != typeof(Parser.ReplayData.GameActions.Action) && parentAction.Parent != null)
            //{
            //    parentAction = parentAction.Parent;
            //}
            //if (parentAction == null || parentAction.Type != typeof(Parser.ReplayData.GameActions.Action))
            //{
            //    return null;
            //}
            //var info = node.Object as Info;
            //var damageTarget = GameState.CurrentEntities[info.Entity];
            //var targetCardId = damageTarget.CardId;
            //var targetControllerId = damageTarget.GetTag(GameTag.CONTROLLER);
            //var damageSource = GetDamageSource(damageTarget, parentAction.Object as Parser.ReplayData.GameActions.Action);
            //var sourceCardId = damageSource.CardId;
            //var sourceControllerId = damageSource.GetTag(GameTag.CONTROLLER);
            //return GameEventProvider.Create(
            //    info.TimeStamp,
            //    () => new GameEvent
            //    {
            //        Type = "DAMAGE",
            //        Value = new
            //        {
            //            SourceCardId = sourceCardId,
            //            SourceControllerId = sourceControllerId,
            //            TargetCardId = targetCardId,
            //            TargetControllerId = targetControllerId,
            //            Damage = (node.Parent.Object as MetaData).Data,
            //            LocalPlayer = ParserState.LocalPlayer,
            //            OpponentPlayer = ParserState.OpponentPlayer,
            //        }
            //    },
            //    true,
            //    node.CreationLogLine);
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            var damageTags = action.Data
                .Where((d) => d.GetType() == typeof(MetaData))
                .Select((meta) => meta as MetaData)
                .Where((meta) => meta.Meta == (int)MetaDataType.DAMAGE);
            Dictionary<string, Dictionary<string, DamageInternal>> totalDamages = new Dictionary<string, Dictionary<string, DamageInternal>>();
            foreach (var damageTag in damageTags)
            {
                foreach (var info in damageTag.MetaInfo)
                {
                    // If source or target are player entities, they don't have any 
                    // attached cardId
                    var damageTarget = GameState.CurrentEntities[info.Entity];
                    var targetCardId = GameState.GetCardIdForEntity(damageTarget.Id);
                    var targetControllerId = damageTarget.GetTag(GameTag.CONTROLLER);
                    var damageSource = GetDamageSource(damageTarget, action);
                    var sourceCardId = GameState.GetCardIdForEntity(damageSource.Id);
                    var sourceControllerId = damageSource.GetTag(GameTag.CONTROLLER);
                    Dictionary<string, DamageInternal> currentSourceDamages = null;
                    if (totalDamages.ContainsKey(sourceCardId))
                    {
                        currentSourceDamages = totalDamages[sourceCardId];
                    }
                    else
                    {
                        currentSourceDamages = new Dictionary<string, DamageInternal>();
                        totalDamages[sourceCardId] = currentSourceDamages;
                    }
                    DamageInternal currentTargetDamages = null;
                    if (currentSourceDamages.ContainsKey(targetCardId))
                    {
                        currentTargetDamages = currentSourceDamages[targetCardId];
                    }
                    else
                    {
                        currentTargetDamages = new DamageInternal
                        {
                            TargetControllerId = targetControllerId,
                            SourceControllerId = sourceControllerId,
                            Damage = 0,
                            Timestamp = info.TimeStamp,
                        };
                        currentSourceDamages[targetCardId] = currentTargetDamages;
                    }
                    currentTargetDamages.Damage = currentTargetDamages.Damage + damageTag.Data;
                }
            }

            List<GameEventProvider> result = new List<GameEventProvider>();
            // Now send one event per source
            foreach (var damageSource in totalDamages.Keys)
            {
                var targetDamages = totalDamages[damageSource];
                var timestamp = totalDamages[damageSource].First().Value.Timestamp;
                result.Add(GameEventProvider.Create(
                    timestamp,
                    () => new GameEvent
                    {
                        Type = "DAMAGE",
                        Value = new
                        {
                            SourceCardId = damageSource,
                            SourceControllerId = totalDamages[damageSource].First().Value.SourceControllerId,
                            Targets = totalDamages[damageSource],
                            LocalPlayer = ParserState.LocalPlayer,
                            OpponentPlayer = ParserState.OpponentPlayer,
                        }
                    },
                    true,
                    node.CreationLogLine));
            }

            return result;
        }

        private bool HasDamageTag(Parser.ReplayData.GameActions.Action action)
        {
            var data = action.Data;
            var numberOfDamageTags = data
                .Where((d) => d.GetType() == typeof(MetaData))
                .Where((meta) => (meta as MetaData).Meta == (int)MetaDataType.DAMAGE)
                .Count();
            return numberOfDamageTags > 0;
        }

        private FullEntity GetDamageSource(FullEntity target, Parser.ReplayData.GameActions.Action action)
        {
            var actionSource = GameState.CurrentEntities[action.Entity];
            if (action.Type == (int)BlockType.ATTACK)
            {
                if (target.Id == action.Entity)
                {
                    return GameState.CurrentEntities[action.Target];
                }
                return GameState.CurrentEntities[action.Entity];
            }
            return actionSource;
        }

        private class DamageInternal
        {
            public int SourceControllerId;
            public int TargetControllerId;
            public int Damage;
            public string Timestamp;
        }
    }
}