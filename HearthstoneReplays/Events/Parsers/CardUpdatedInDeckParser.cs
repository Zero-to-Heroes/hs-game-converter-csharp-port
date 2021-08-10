using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using HearthstoneReplays.Parser.ReplayData.Meta;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardUpdatedInDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardUpdatedInDeckParser(ParserState ParserState)
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
            var appliesOnShow = node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.DECK
                && GameState.CurrentEntities.ContainsKey((node.Object as ShowEntity).Entity)
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK;
            var appliesForAction = node.Type == typeof(Action)
                && GameState.CurrentEntities.ContainsKey((node.Object as Action).Entity)
                && GameState.CurrentEntities[(node.Object as Action).Entity].CardId == CardIds.NonCollectible.Rogue.FindtheImposter_SpyOMaticToken;
            return appliesOnShow || appliesForAction;
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
            else if (node.Type == typeof(Action))
            {
                return CreateFromAction(node);
            }
            return null;
        }

        private List<GameEventProvider> CreateFromAction(Node node)
        {
            var action = node.Object as Action;
            var changedEntities = action.Data
                .Where(data => data.GetType() == typeof(MetaData))
                .Select(data => data as MetaData)
                .Where(meta => meta.Meta == (int)MetaDataType.HISTORY_TARGET)
                .SelectMany(meta => meta.MetaInfo)
                .ToList();

            return changedEntities
                .Select(info =>
                {
                    var entityId = info.Entity;
                    var entity = GameState.CurrentEntities[entityId];
                    return GameEventProvider.Create(
                        info.TimeStamp,
                        "CARD_CHANGED_IN_DECK",
                        GameEvent.CreateProvider(
                            "CARD_CHANGED_IN_DECK",
                            entity.CardId,
                            entity.GetController(),
                            entity.Id,
                            ParserState,
                            GameState,
                            null,
                            new
                            {
                                LastInfluencedByCardId = GameState.CurrentEntities[(node.Object as Action).Entity].CardId,
                            }),
                        true,
                        node);
                })
                .ToList();
        }

        private List<GameEventProvider> CreateFromShowEntity(Node node) { 
            var showEntity = node.Object as ShowEntity;
            // Cards here are just created to show the info, then put aside. We don't want to 
            // show them in the "Other" zone, so we just ignore them
            if (showEntity.SubSpellInEffect?.Prefab == "DMFFX_SpawnToDeck_CthunTheShattered_CardFromScript_FX")
            {
                return null;
            }

            var cardId = showEntity.CardId;
            var entity = GameState.CurrentEntities[showEntity.Entity];
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            var creatorEntityId = showEntity.GetTag(GameTag.CREATOR);
            var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId)
                ? GameState.CurrentEntities[creatorEntityId].CardId
                : null;
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                "CARD_CHANGED_IN_DECK",
                GameEvent.CreateProvider(
                    "CARD_CHANGED_IN_DECK",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        CreatorCardId = creatorEntityCardId,
                    }),
                true,
                node) };
        }
    }
}
