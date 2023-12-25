using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

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
                && (GameState.CurrentEntities[(node.Object as TagChange).Entity]?.GetTag(GameTag.ZONE) == (int)Zone.SETASIDE
                    // Nagaling creates the secret in the REMOVEDFROMGAME zone first
                    || GameState.CurrentEntities[(node.Object as TagChange).Entity]?.GetTag(GameTag.ZONE) == (int)Zone.REMOVEDFROMGAME);
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            var appliesFullEntity = stateType == StateType.PowerTaskList
                && node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.SECRET;
            // For Nagaling
            var appliesShowEntity = stateType == StateType.PowerTaskList
                && node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.SECRET
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity]?.GetTag(GameTag.ZONE) == (int)Zone.REMOVEDFROMGAME;
            return appliesFullEntity || appliesShowEntity;
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
            if (node.Object is FullEntity)
            {
                return CreateGameEventProviderFromFullEntity(node);
            }
            else if (node.Object is ShowEntity)
            {
                return CreateGameEventProviderFromShowEntity(node);
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromFullEntity(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var controllerId = fullEntity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, null);
            var playerClass = fullEntity.GetPlayerClass();
            var creator = Oracle.FindCardCreator(GameState, fullEntity, node);
            //var creatorEntityId = fullEntity.GetTag(GameTag.CREATOR);
            //var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId)
            //    ? GameState.CurrentEntities[creatorEntityId].CardId
            //    : null;
            var eventName = fullEntity.GetTag(GameTag.SECRET) == 1
                ? "SECRET_CREATED_IN_GAME"
                : "QUEST_CREATED_IN_GAME";
            var cardId = fullEntity.CardId;
            if (cardId.Length == 0 && fullEntity.GetTag(GameTag.SECRET) == 1 && creator != null)
            {
                cardId = Oracle.PredictCardId(GameState, creator.Item1, creator.Item2, node, fullEntity.CardId);
            }
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
                        CreatorCardId = creator?.Item1,
                    }),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromShowEntity(Node node)
        {
            var showEntity = node.Object as ShowEntity;
            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, null);
            var playerClass = showEntity.GetPlayerClass();
            var creatorEntityId = showEntity.GetTag(GameTag.CREATOR);
            var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId)
                ? GameState.CurrentEntities[creatorEntityId].CardId
                : null;
            var eventName = showEntity.GetTag(GameTag.SECRET) == 1
                ? "SECRET_CREATED_IN_GAME"
                : "QUEST_CREATED_IN_GAME";
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                eventName,
                GameEvent.CreateProvider(
                    eventName,
                    cardId,
                    controllerId,
                    showEntity.Entity,
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
