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
            var gameState= GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            // If we compute this when triggering the event, we will get a "gift" icon because the 
            // card is already in hand
            var wasInDeck = entity.GetTag(GameTag.ZONE) == (int)Zone.DECK;
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "CARD_DRAW_FROM_DECK",
                () => {
                    var entityId = tagChange.Entity;
                    // We do it here because of Keymaster Alabaster - we need to know the last card
                    // that has been drawn
                    var creatorCardId = wasInDeck ? null : Oracle.FindCardCreatorCardId(GameState, entity, node, false);
                    // Always return this info, and the client has a list of public card creators they are allowed to show
                    var lastInfluencedByCardId = Oracle.FindCardCreatorCardId(GameState, entity, node);
                    GameState.OnCardDrawn(entity.Entity);
                    return new GameEvent
                    {
                        Type =  "CARD_DRAW_FROM_DECK",
                        Value = new
                        {
                            CardId = cardId,
                            ControllerId = controllerId,
                            LocalPlayer = ParserState.LocalPlayer,
                            OpponentPlayer = ParserState.OpponentPlayer,
                            EntityId = entity.Id,
                            GameState = gameState,
                            AdditionalProps = new {
                                IsPremium = entity.GetTag(GameTag.PREMIUM) == 1,
                                CreatorCardId = creatorCardId,
                                LastInfluencedByCardId = lastInfluencedByCardId,
                            }
                        }
                    };
                },
                true,
                node.CreationLogLine) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateEventFromShowEntity(node.Object as ShowEntity, node.CreationLogLine, node);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateEventFromFullEntity(node.Object as FullEntity, node.CreationLogLine, node);
            }
            return null;
        }

        private List<GameEventProvider> CreateEventFromShowEntity(ShowEntity showEntity, string creationLogLine, Node node)
        {
            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            var entity = GameState.CurrentEntities[showEntity.Entity];
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
            var wasInDeck = entity.GetTag(GameTag.ZONE) == (int)Zone.DECK;
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                "CARD_DRAW_FROM_DECK",
                () => {
                    // We do it here because of Keymaster Alabaster - we need to know the last card
                    // that has been drawn
                    var creatorCardId = wasInDeck ? null : Oracle.FindCardCreatorCardId(GameState, showEntity, node);
                    var lastInfluencedByCardId = Oracle.FindCardCreatorCardId(GameState, showEntity, node);
                    GameState.OnCardDrawn(showEntity.Entity);
                    return new GameEvent
                    {
                        Type =  "CARD_DRAW_FROM_DECK",
                        Value = new
                        {
                            CardId = cardId,
                            ControllerId = controllerId,
                            LocalPlayer = ParserState.LocalPlayer,
                            OpponentPlayer = ParserState.OpponentPlayer,
                            EntityId = showEntity.Entity,
                            GameState = gameState,
                            AdditionalProps = new {
                                IsPremium = entity.GetTag(GameTag.PREMIUM) == 1 || showEntity.GetTag(GameTag.PREMIUM) == 1,
                                CreatorCardId = creatorCardId,
                                LastInfluencedByCardId = lastInfluencedByCardId,
                            }
                        }
                    };
                },
                true,
                creationLogLine) };
        }

        private List<GameEventProvider> CreateEventFromFullEntity(FullEntity fullEntity, string creationLogLine, Node node)
        {
            var cardId = fullEntity.CardId;
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            var wasInDeck = fullEntity.GetTag(GameTag.ZONE) == (int)Zone.DECK;
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                "CARD_DRAW_FROM_DECK",
                () => {
                    // We do it here because of Keymaster Alabaster - we need to know the last card
                    // that has been drawn
                    var creatorCardId = wasInDeck ? null : Oracle.FindCardCreatorCardId(GameState, fullEntity, node, false);
                    var lastInfluencedByCardId = Oracle.FindCardCreatorCardId(GameState, fullEntity, node);
                    GameState.OnCardDrawn(fullEntity.Entity);
                    return new GameEvent
                    {
                        Type =  "CARD_DRAW_FROM_DECK",
                        Value = new
                        {
                            CardId = cardId,
                            ControllerId = controllerId,
                            LocalPlayer = ParserState.LocalPlayer,
                            OpponentPlayer = ParserState.OpponentPlayer,
                            EntityId = fullEntity.Entity,
                            GameState = gameState,
                            AdditionalProps = new {
                                IsPremium = fullEntity.GetTag(GameTag.PREMIUM) == 1 || fullEntity.GetTag(GameTag.PREMIUM) == 1,
                                CreatorCardId = creatorCardId,
                                LastInfluencedByCardId = lastInfluencedByCardId,
                            }
                        }
                    };
                },
                true,
                creationLogLine) };
        }
    }
}
