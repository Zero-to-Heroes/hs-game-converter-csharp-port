using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class MercenariesAbilityCooldownUpdatedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public MercenariesAbilityCooldownUpdatedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            TagChange tagChange = null;
            return node.Type == typeof(TagChange)
                && (tagChange = node.Object as TagChange).Name == (int)GameTag.LETTUCE_CURRENT_COOLDOWN;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            var newCooldownValue = tagChange.Value;
            var abilityOwner = entity.GetTag(GameTag.LETTUCE_ABILITY_OWNER);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "MERCENARIES_COOLDOWN_UPDATED",
                GameEvent.CreateProvider(
                    "MERCENARIES_COOLDOWN_UPDATED",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    null,
                    new {
                        NewCooldown = newCooldownValue,
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
