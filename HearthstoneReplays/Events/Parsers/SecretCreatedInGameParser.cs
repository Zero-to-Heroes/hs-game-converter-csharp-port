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
        private StateFacade StateFacade { get; set; }

        public SecretCreatedInGameParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Value == (int)Zone.SECRET
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && GameState.CurrentEntities[(node.Object as TagChange).Entity]?.GetTag(GameTag.ZONE) == (int)Zone.SETASIDE;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(FullEntity)
                 && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.SECRET;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            if (GameState.CurrentEntities[tagChange.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var eventName = GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.SECRET) == 1
                    ? "SECRET_CREATED_IN_GAME"
                    : "QUEST_CREATED_IN_GAME";
                var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, tagChange, null);
                var playerClass = entity.GetPlayerClass();
                var creatorEntityId = entity.GetTag(GameTag.CREATOR);
                if (creatorEntityId == -1)
                {
                    creatorEntityId = entity.GetTag(GameTag.DISPLAYED_CREATOR);
                }
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
                            StateFacade,
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
            var controllerId = fullEntity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, null);
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
                    StateFacade,
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
