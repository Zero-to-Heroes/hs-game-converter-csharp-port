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
                && GameState.CurrentEntities[showEntity.GetTag(GameTag.LAST_AFFECTED_BY)].CardId == CardIds.Collectible.Paladin.OhMyYogg)
            {
                return null;
            }

            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                "ENTITY_UPDATE",
                GameEvent.CreateProvider(
                    "ENTITY_UPDATE",
                    cardId,
                    controllerId,
                    showEntity.Entity,
                    ParserState,
                    GameState,
                    gameState),
                true,
                node) };
        }
    }
}
