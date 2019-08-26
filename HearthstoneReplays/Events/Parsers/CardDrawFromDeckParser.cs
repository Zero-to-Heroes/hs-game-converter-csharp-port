using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardDrawFromDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardDrawFromDeckParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.HAND
                && (GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK 
                    || GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == -1);
        }

        public bool AppliesOnCloseNode(Node node)
        {
            var appliesToShowEntity = node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.HAND
                && GameState.CurrentEntities.ContainsKey((node.Object as ShowEntity).Entity)
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK;
            var appliesToFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.HAND
                && GameState.CurrentEntities.ContainsKey((node.Object as FullEntity).Id)
                && GameState.CurrentEntities[(node.Object as FullEntity).Id].GetTag(GameTag.ZONE) == (int)Zone.DECK;
            return appliesToShowEntity || appliesToFullEntity;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                GameEvent.CreateProvider(
                    "CARD_DRAW_FROM_DECK",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                        GameState,
                    gameState),
                true,
                node.CreationLogLine) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateEventFromShowEntity(node.Object as ShowEntity, node.CreationLogLine);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateEventFromFullEntity(node.Object as FullEntity, node.CreationLogLine);
            }
            return null;
        }

        private List<GameEventProvider> CreateEventFromShowEntity(ShowEntity showEntity, string creationLogLine)
        {
            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                GameEvent.CreateProvider(
                    "CARD_DRAW_FROM_DECK",
                    cardId,
                    controllerId,
                    showEntity.Entity,
                    ParserState,
                        GameState,
                    gameState),
                true,
                creationLogLine) };
        }

        private List<GameEventProvider> CreateEventFromFullEntity(FullEntity fullEntity, string creationLogLine)
        {
            var cardId = fullEntity.CardId;
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                GameEvent.CreateProvider(
                    "CARD_DRAW_FROM_DECK",
                    cardId,
                    controllerId,
                    fullEntity.Id,
                    ParserState,
                        GameState,
                    gameState),
                true,
                creationLogLine) };
        }
    }
}
