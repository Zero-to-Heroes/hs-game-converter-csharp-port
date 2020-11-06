using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class DiscardedCardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public DiscardedCardParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.GRAVEYARD
                && GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == (int)Zone.HAND;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.GRAVEYARD
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) == (int)Zone.HAND;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "DISCARD_CARD",
                GameEvent.CreateProvider(
                    "DISCARD_CARD",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    gameState),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var showEntity = node.Object as ShowEntity;
            var entity = GameState.CurrentEntities[showEntity.Entity];
            if (entity == null)
            {
                Logger.Log("Could not find entity while looking for discard", showEntity.Entity);
            }
            var cardId = entity?.CardId != null && entity.CardId.Length > 0 ? entity.CardId : showEntity.CardId;
            var controllerId = entity != null ? entity.GetTag(GameTag.CONTROLLER) : -1;
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                "DISCARD_CARD",
                GameEvent.CreateProvider(
                    "DISCARD_CARD",
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
