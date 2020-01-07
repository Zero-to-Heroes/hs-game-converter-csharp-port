using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class ReceiveCardInHandParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public ReceiveCardInHandParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange) 
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.HAND
                && GameState.CurrentEntities.ContainsKey((node.Object as TagChange).Entity)
                && GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) != (int)Zone.DECK;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            var appliesToShowEntity = node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.HAND
                && (!GameState.CurrentEntities.ContainsKey((node.Object as ShowEntity).Entity)
                    || (GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) != (int)Zone.DECK
                        && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) != (int)Zone.HAND));
            var appliesToFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.HAND
                && (!GameState.CurrentEntities.ContainsKey((node.Object as FullEntity).Id)
                    || (GameState.CurrentEntities[(node.Object as FullEntity).Id].GetTag(GameTag.ZONE) != (int)Zone.DECK
                        && GameState.CurrentEntities[(node.Object as FullEntity).Id].GetTag(GameTag.ZONE) != (int)Zone.HAND));
            return appliesToShowEntity || appliesToFullEntity;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var previousZone = entity.GetTag(GameTag.ZONE) == -1 ? 0 : entity.GetTag(GameTag.ZONE);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            
            return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    GameEvent.CreateProvider(
                        "RECEIVE_CARD_IN_HAND",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            IsPremium = entity.GetTag(GameTag.PREMIUM) == 1
                        }),
                    true,
                    node.CreationLogLine) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateEventFromShowEntity(node, node.CreationLogLine);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateEventFromFullEntity(node, node.CreationLogLine);
            }
            return null;
        }

        private List<GameEventProvider> CreateEventFromShowEntity(Node node, string creationLogLine)
        {
            ShowEntity showEntity = node.Object as ShowEntity;
            var creatorCardId = Oracle.FindCardCreatorCardId(GameState, showEntity.GetTag(GameTag.CREATOR), node);
            var cardId = Oracle.PredictCardId(GameState, creatorCardId, node, showEntity.CardId);
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            var previousZone = GameState.CurrentEntities[showEntity.Entity].GetTag(GameTag.ZONE);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            var entity = GameState.CurrentEntities[showEntity.Entity];
            return new List<GameEventProvider> { GameEventProvider.Create(
                    showEntity.TimeStamp,
                    GameEvent.CreateProvider(
                        "RECEIVE_CARD_IN_HAND",
                        cardId,
                        controllerId,
                        showEntity.Entity,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            CreatorCardId = creatorCardId, // Used when there is no cardId, so we can show at least the card that created it
                            IsPremium = entity.GetTag(GameTag.PREMIUM) == 1 || showEntity.GetTag(GameTag.PREMIUM) == 1
                        }),
                    true,
                    creationLogLine) };
        }

        private List<GameEventProvider> CreateEventFromFullEntity(Node node, string creationLogLine)
        {
            FullEntity fullEntity = node.Object as FullEntity;
            var creatorCardId = Oracle.FindCardCreatorCardId(GameState, fullEntity.GetTag(GameTag.CREATOR), node);
            var cardId = Oracle.PredictCardId(GameState, creatorCardId, node, fullEntity.CardId);
            if (cardId == null && GameState.CurrentTurn == 1 && fullEntity.GetTag(GameTag.ZONE_POSITION) == 5)
            {
                var controller = GameState.GetController(fullEntity.GetTag(GameTag.CONTROLLER));
                if (controller.GetTag(GameTag.CURRENT_PLAYER) != 1)
                {
                    cardId = "GAME_005";
                }
            }
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var previousZone = 0;
            if (GameState.CurrentEntities.ContainsKey(fullEntity.Id))
            {
                previousZone = GameState.CurrentEntities[fullEntity.Id].GetTag(GameTag.ZONE);
            }
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                    fullEntity.TimeStamp,
                    GameEvent.CreateProvider(
                        "RECEIVE_CARD_IN_HAND",
                        cardId,
                        controllerId,
                        fullEntity.Id,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            CreatorCardId = creatorCardId,
                            IsPremium = fullEntity.GetTag(GameTag.PREMIUM) == 1,
                        }),
                    true,
                    creationLogLine) };
        }
    }
}
