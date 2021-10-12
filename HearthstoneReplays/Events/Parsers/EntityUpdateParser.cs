using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class EntityUpdateParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public EntityUpdateParser(ParserState ParserState)
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
            return node.Type == typeof(ShowEntity);
                // We need this so that cards that were unknown in the opponent's hand can be assigned
                // their info
                //&& !MindrenderIlluciaParser.IsProcessingMindrenderIlluciaEffect(node, GameState);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        // Typically the case when the opponent plays a quest or a secret
        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var showEntity = node.Object as ShowEntity;
            // Cards here are just created to show the info, then put aside. We don't want to 
            // show them in the "Other" zone, so we just ignore them
            if (showEntity.SubSpellInEffect?.Prefab == "DMFFX_SpawnToDeck_CthunTheShattered_CardFromScript_FX")
            {
                return null;
            }

            // CArds transformed by Oh My Yogg are instead reemitted as new card played
            if (showEntity.GetTag(GameTag.LAST_AFFECTED_BY) != -1 
                && GameState.CurrentEntities.ContainsKey(showEntity.GetTag(GameTag.LAST_AFFECTED_BY))
                && GameState.CurrentEntities[showEntity.GetTag(GameTag.LAST_AFFECTED_BY)].CardId == CardIds.Collectible.Paladin.OhMyYogg)
            {
                return null;
            }

            var cardId = showEntity.CardId;
            // Because Encumbered Pack Mule reveals itself if drawn during mulligan, we need to 
            // have a special rule
            var isBeforeMulligan = GameState.GetGameEntity().GetTag(GameTag.NEXT_STEP) == -1;
            if (isBeforeMulligan && cardId == CardIds.Collectible.Neutral.EncumberedPackMule)
            {
                cardId = "";
            }

            var controllerId = showEntity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
            var mercXp = showEntity.GetTag(GameTag.LETTUCE_MERCENARY_EXPERIENCE);
            var mercEquipmentId = showEntity.GetTag(GameTag.LETTUCE_EQUIPMENT_ID);
            var abilityOwner = showEntity.GetTag(GameTag.LETTUCE_ABILITY_OWNER);
            var abilityCooldownConfig = showEntity.GetTag(GameTag.LETTUCE_COOLDOWN_CONFIG);
            var abilityCurrentCooldown = showEntity.GetTag(GameTag.LETTUCE_CURRENT_COOLDOWN);
            var abilitySpeed = showEntity.GetTag(GameTag.COST);
            var eventName = showEntity.GetTag(GameTag.ZONE) == (int)Zone.LETTUCE_ABILITY
                ? showEntity.GetTag(GameTag.LETTUCE_IS_EQUPIMENT) == 1
                    ? "MERCENARIES_EQUIPMENT_UPDATE"
                    : "MERCENARIES_ABILITY_UPDATE"
                : "ENTITY_UPDATE";
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                eventName,
                GameEvent.CreateProvider(
                    eventName,
                    cardId,
                    controllerId,
                    showEntity.Entity,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        MercenariesExperience = mercXp,
                        MercenariesEquipmentId = mercEquipmentId,
                        AbilityOwnerEntityId = abilityOwner,
                        AbilityCooldownConfig = abilityCooldownConfig == -1 ? (int?)null : abilityCooldownConfig,
                        AbilityCurrentCooldown = abilityCurrentCooldown == -1 ? (int?)null : abilityCurrentCooldown,
                        AbilitySpeed = abilitySpeed == -1 ? (int?)null : abilitySpeed,
                    }),
                true,
                node,
                // For some reason, the event is not sent because of missing animlation log
                // (I still don't understand why)
                false,
                false,
                false,
                // See comments in NodeParser
                new
                {
                    Mindrender = true,
                }
            )};
        }
    }
}
