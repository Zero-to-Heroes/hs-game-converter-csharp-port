using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class SecretCreatedInGameParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public SecretCreatedInGameParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Value == (int)Zone.SECRET
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && GameState.CurrentEntities[(node.Object as TagChange).Entity]?.GetTag(GameTag.ZONE) == (int)Zone.SETASIDE;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(FullEntity)
                 && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.SECRET;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            if (GameState.CurrentEntities[tagChange.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var eventName = GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.SECRET) == 1
                    ? "SECRET_CREATED_IN_GAME"
                    : "QUEST_CREATED_IN_GAME";
                var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
                var playerClass = entity.GetPlayerClass();
                var creatorEntityId = entity.GetTag(GameTag.CREATOR);
                var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId)
                    ? GameState.CurrentEntities[creatorEntityId].CardId
                    : null;
                return new List<GameEventProvider> { GameEventProvider.Create(
                        tagChange.TimeStamp,
                        eventName,
                        GameEvent.CreateProvider(
                            eventName,
                            cardId,
                            controllerId,
                            entity.Id,
                            ParserState,
                            GameState,
                            gameState,
                            new {
                                PlayerClass = playerClass,
                                CreatorCardId = creatorEntityCardId,
                            }),
                       true,
                       node) };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var cardId = fullEntity.CardId;
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            var playerClass = fullEntity.GetPlayerClass();
            var creatorEntityId = fullEntity.GetTag(GameTag.CREATOR);
            var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId)
                ? GameState.CurrentEntities[creatorEntityId].CardId
                : null;
            var eventName = fullEntity.GetTag(GameTag.SECRET) == 1
                ? "SECRET_CREATED_IN_GAME"
                : "QUEST_CREATED_IN_GAME";
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                eventName,
                GameEvent.CreateProvider(
                    eventName,
                    cardId,
                    controllerId,
                    fullEntity.Entity,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        PlayerClass = playerClass,
                        CreatorCardId = creatorEntityCardId,
                    }),
                true,
                node) };
        }
    }
}
