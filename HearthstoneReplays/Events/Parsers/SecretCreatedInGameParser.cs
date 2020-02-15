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
            return false;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(FullEntity)
                 && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.SECRET;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var cardId = fullEntity.CardId;
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            var playerClass = fullEntity.GetPlayerClass();
            var creatorEntityId = fullEntity.GetTag(GameTag.CREATOR);
            var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId)
                ? GameState.CurrentEntities[creatorEntityId].CardId
                : null;
            var eventName = fullEntity.GetTag(GameTag.QUEST) == 1
                ? "QUEST_CREATED_IN_GAME"
                : "SECRET_CREATED_IN_GAME";
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
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
                node.CreationLogLine) };
        }
    }
}
