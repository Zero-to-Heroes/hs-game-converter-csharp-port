using HearthstoneReplays.Parser;
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
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.Action)
                && HasDamageTag(node.Object as Parser.ReplayData.GameActions.Action);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
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
                    var targetEntityId = damageTarget.Id;
                    var targetCardId = GameState.GetCardIdForEntity(damageTarget.Id);
                    var targetControllerId = damageTarget.GetTag(GameTag.CONTROLLER);
                    var damageSource = GetDamageSource(damageTarget, action, damageTag);
                    var sourceEntityId = damageSource.Id;
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
                            SourceEntityId = sourceEntityId,
                            SourceControllerId = sourceControllerId,
                            TargetEntityId = targetEntityId,
                            TargetControllerId = targetControllerId,
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
                    // The structure of this event is too specific to be added to the generic GameEvent.CreateProvider() method
                    () => new GameEvent
                    {
                        Type = "DAMAGE",
                        Value = new
                        {
                            SourceCardId = damageSource,
                            SourceEntityId = totalDamages[damageSource].First().Value.SourceEntityId,
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

        private FullEntity GetDamageSource(FullEntity target, Parser.ReplayData.GameActions.Action action, MetaData meta)
        {
            var actionSource = GameState.CurrentEntities[action.Entity];
            if (action.Type == (int)BlockType.ATTACK)
            {
                var damageSource = action.Entity;
                if (target.Id == action.Entity)
                {
                    // This doesn't work, because once the action is ended the PROPOSED_DEFENDER is reset
                    // var defender = GameState.GetGameEntity().GetTag(GameTag.PROPOSED_DEFENDER);
                    // We still want to handle the attack at the global level (so that we can group 
                    // damage events), so we need to use a trick
                    var metaIndex = action.Data.IndexOf(meta);
                    for (var i = 0; i < metaIndex; i++)
                    {
                        var data = action.Data[i];
                        if (data.GetType() == typeof(TagChange) 
                            && (data as TagChange).Name == (int)GameTag.PROPOSED_DEFENDER
                            && (data as TagChange).Entity == GameState.GetGameEntity().Id)
                        {
                            damageSource = (data as TagChange).Value;
                        }
                    }
                }
                return GameState.CurrentEntities[damageSource];
            }
            return actionSource;
        }

        private class DamageInternal
        {
            public int SourceEntityId;
            public int SourceControllerId;
            public int TargetEntityId;
            public int TargetControllerId;
            public int Damage;
            public DateTime Timestamp;
        }
    }
}
