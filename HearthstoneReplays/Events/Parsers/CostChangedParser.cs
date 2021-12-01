using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class CostChangedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CostChangedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            // This is for now only useful to get the speed update in Mercenaries, so we try to restrict the number of events
            return ParserState.IsMercenaries()
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.COST;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            FullEntity entity = GameState.CurrentEntities.TryGetValue(tagChange.Entity, out entity) ? entity : null;
            if (entity == null)
            {
                return null;
            }

            var cardId = string.IsNullOrEmpty(entity.CardId) ? null : entity.CardId;
            var controllerId = entity.GetEffectiveController();
            var abilityOwner = entity.GetTag(GameTag.LETTUCE_ABILITY_OWNER);
            return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "COST_CHANGED",
                    GameEvent.CreateProvider(
                        "COST_CHANGED",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        null,
                        new {
                            NewCost = tagChange.Value,
                            AbilityOwnerEntityId = abilityOwner,
                        }),
                    true,
                    node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
