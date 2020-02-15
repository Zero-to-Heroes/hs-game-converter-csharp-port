using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class SecretPlayedFromHandParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public SecretPlayedFromHandParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.SECRET
                && GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == (int)Zone.HAND;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.Action)
                && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.PLAY;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            if (GameState.CurrentEntities[tagChange.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var eventName = GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.QUEST) == 1
                    ? "QUEST_PLAYED"
                    : "SECRET_PLAYED";
                var gameState = GameEvent.BuildGameState(ParserState, GameState);
                var playerClass = entity.GetPlayerClass();
                return new List<GameEventProvider> { GameEventProvider.Create(
                       tagChange.TimeStamp,
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
                            }),
                       true,
                       node.CreationLogLine) };
            }
            return null;
        }

        // Typically the case when the opponent plays a quest or a secret
        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            foreach (var data in action.Data)
            {
                if (data.GetType() == typeof(ShowEntity))
                {
                    var showEntity = data as ShowEntity;
                    if (showEntity.GetTag(GameTag.ZONE) == (int)Zone.SECRET
                        && showEntity.GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
                    {
                        var cardId = showEntity.CardId;
                        var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
                        var gameState = GameEvent.BuildGameState(ParserState, GameState);
                        var playerClass = showEntity.GetPlayerClass();
                        var eventName = showEntity.GetTag(GameTag.QUEST) == 1
                            ? "QUEST_PLAYED"
                            : "SECRET_PLAYED";
                        // For now there can only be one card played per block
                        return new List<GameEventProvider> { GameEventProvider.Create(
                            action.TimeStamp,
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
                            node.CreationLogLine) };
                    }
                }
            }
            return null;
        }
    }
}
