using HearthstoneReplays.Parser;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class MercenariesAbilityRevealedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public MercenariesAbilityRevealedParser(ParserState ParserState)
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
            var isCorrectType = node.Type == typeof(FullEntity);
            return node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.LETTUCE_ABILITY;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            if (fullEntity.GetCardType() != (int)CardType.LETTUCE_ABILITY)
            {
                return null;
            }

            var abilityOwner = fullEntity.GetTag(GameTag.LETTUCE_ABILITY_OWNER);
            var abilityCooldownConfig = fullEntity.GetTag(GameTag.LETTUCE_COOLDOWN_CONFIG);
            var abilityCurrentCooldown = fullEntity.GetTag(GameTag.LETTUCE_CURRENT_COOLDOWN);
            var abilitySpeed = fullEntity.GetTag(GameTag.COST);
            var controllerId = fullEntity.GetEffectiveController();
            var cardId = fullEntity.CardId;
            var eventName = fullEntity.GetTag(GameTag.LETTUCE_IS_EQUPIMENT) == 1 ? "MERCENARIES_EQUIPMENT_REVEALED" : "MERCENARIES_ABILITY_REVEALED";
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                eventName,
                GameEvent.CreateProvider(
                    eventName,
                    cardId,
                    controllerId,
                    fullEntity.Id,
                    ParserState,
                    GameState,
                    null,
                    new {
                        AbilityOwnerEntityId = abilityOwner,
                        AbilityCooldownConfig = abilityCooldownConfig == -1 ? (int?)null : abilityCooldownConfig,
                        AbilityCurrentCooldown = abilityCurrentCooldown == -1 ? (int?)null : abilityCurrentCooldown,
                        AbilitySpeed = abilitySpeed == -1 ? (int?)null : abilitySpeed,
                    }
                ),
                true,
                node) };
        }
    }
}
