using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class SecretDestroyedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public SecretDestroyedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.GRAVEYARD
                && GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == (int)Zone.SECRET;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            // When the active player destroys a secret, it is fully revealed
            var appliesToShowEntity = node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.GRAVEYARD
                && GameState.CurrentEntities.ContainsKey((node.Object as ShowEntity).Entity)
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) == (int)Zone.SECRET;
            return appliesToShowEntity;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            var eventName = entity.GetTag(GameTag.QUEST) == 1 || entity.GetTag(GameTag.SIDEQUEST) == 1
                ? "QUEST_DESTROYED"
                : "SECRET_DESTROYED";
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
                    gameState),
                true,
                node) };
        }

        // Typically the case when the opponent plays a quest or a secret
        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var showEntity = node.Object as ShowEntity;
            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
            var playerClass = showEntity.GetPlayerClass();
            var eventName = showEntity.GetTag(GameTag.QUEST) == 1 || showEntity.GetTag(GameTag.SIDEQUEST) == 1
                ? "QUEST_DESTROYED"
                : "SECRET_DESTROYED";
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
                        PlayerClass = playerClass,
                    }),
                true,
                node) };
        }
    }
}
