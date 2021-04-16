using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using static HearthstoneReplays.Events.CardIds.Collectible;
using HearthstoneReplays.Parser.ReplayData.Meta;

namespace HearthstoneReplays.Events.Parsers
{
    public class CreateCardInDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CreateCardInDeckParser(ParserState ParserState)
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
            // In this case, a FullEntity is created with minimal info, and the real
            // card creation happens in the ShowEntity
            var appliesOnShowEntity = node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.DECK;
            var appliesOnFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.DECK
                // We don't want to include the entities created when the game starts
                && node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action);
            return appliesOnShowEntity || appliesOnFullEntity;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateFromShowEntity(node);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateFromFullEntity(node);
            }
            return null;
        }

        private List<GameEventProvider> CreateFromShowEntity(Node node)
        {
            var showEntity = node.Object as ShowEntity;
            // Cards here are just created to show the info, then put aside. We don't want to 
            // show them in the "Other" zone, so we just ignore them
            if (showEntity.SubSpellInEffect?.Prefab == "DMFFX_SpawnToDeck_CthunTheShattered_CardFromScript_FX")
            {
                return null;
            }

            var currentCard = GameState.CurrentEntities[showEntity.Entity];
            // If the card is already present in the deck, do nothing
            if (currentCard.GetTag(GameTag.ZONE) == (int)Zone.DECK)
            {
                return null;
            }

            var creatorCardId = Oracle.FindCardCreatorCardId(GameState, showEntity, node);
            var creatorEntityId = Oracle.FindCardCreatorEntityId(GameState, showEntity, node);
            var cardId = Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, showEntity.CardId);
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                "CREATE_CARD_IN_DECK",
                GameEvent.CreateProvider(
                    "CREATE_CARD_IN_DECK",
                    cardId,
                    controllerId,
                    showEntity.Entity,
                    ParserState,
                    GameState,
                    new {
                        CreatorCardId = creatorCardId, // Used when there is no cardId, so we can show at least the card that created it
                    }),
                true,
                node) };
        }

        private List<GameEventProvider> CreateFromFullEntity(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            // Cards here are just created to show the info, then put aside. We don't want to 
            // show them in the "Other" zone, so we just ignore them
            if (fullEntity.SubSpellInEffect?.Prefab == "DMFFX_SpawnToDeck_CthunTheShattered_CardFromScript_FX")
            {
                return null;
            }

            var creatorCardId = Oracle.FindCardCreatorCardId(GameState, fullEntity, node);
            var creatorEntityId = Oracle.FindCardCreatorEntityId(GameState, fullEntity, node);
            var cardId = Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, fullEntity.CardId);
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                "CREATE_CARD_IN_DECK",
                GameEvent.CreateProvider(
                    "CREATE_CARD_IN_DECK",
                    cardId,
                    controllerId,
                    fullEntity.Id,
                    ParserState,
                        GameState,
                    gameState,
                    new {
                        CreatorCardId = creatorCardId, // Used when there is no cardId, so we can show "created by ..."
                    }),
                true,
                node) };
        }
    }
}
