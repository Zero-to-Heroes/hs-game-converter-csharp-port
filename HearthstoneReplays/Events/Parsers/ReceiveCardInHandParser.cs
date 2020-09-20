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
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);

            return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "RECEIVE_CARD_IN_HAND",
                    GameEvent.CreateProvider(
                        "RECEIVE_CARD_IN_HAND",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            IsPremium = entity.GetTag(GameTag.PREMIUM) == 1,
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
            //Logger.Log("Will add creator " + showEntity.GetTag(GameTag.CREATOR) + " //" + showEntity.GetTag(GameTag.DISPLAYED_CREATOR), "");
            var creatorCardId = Oracle.FindCardCreatorCardId(GameState, showEntity, node);
            var creatorEntityId = Oracle.FindCardCreatorEntityId(GameState, showEntity, node);
            var cardId = Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, showEntity.CardId);
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            var previousZone = GameState.CurrentEntities[showEntity.Entity].GetTag(GameTag.ZONE);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
            var entity = GameState.CurrentEntities[showEntity.Entity];
            // Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, showEntity.CardId);
            return new List<GameEventProvider> { GameEventProvider.Create(
                    showEntity.TimeStamp,
                    "RECEIVE_CARD_IN_HAND",
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
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var previousZone = 0;
            if (GameState.CurrentEntities.ContainsKey(fullEntity.Id))
            {
                previousZone = GameState.CurrentEntities[fullEntity.Id].GetTag(GameTag.ZONE);
            }
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                    fullEntity.TimeStamp,
                    "RECEIVE_CARD_IN_HAND",
                    () => {
                        // We do it here because of Diligent Notetaker - we have to know the last
                        // card played before assigning anything
                        var creatorCardId = Oracle.FindCardCreatorCardId(GameState, fullEntity, node);
                        var creatorEntityId = Oracle.FindCardCreatorEntityId(GameState, fullEntity, node);
                        var cardId = Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, fullEntity.CardId);
                        if (cardId == null && GameState.CurrentTurn == 1 && fullEntity.GetTag(GameTag.ZONE_POSITION) == 5)
                        {
                            var controller = GameState.GetController(fullEntity.GetTag(GameTag.CONTROLLER));
                            if (controller.GetTag(GameTag.CURRENT_PLAYER) != 1)
                            {
                                cardId = "GAME_005";
                                creatorCardId = "GAME_005";
                            }
                        }
                        //var a = "t";
                        //Oracle.FindCardCreatorCardId(GameState, fullEntity, node);
                        //Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, fullEntity.CardId);
                        return new GameEvent
                        {
                            Type =  "RECEIVE_CARD_IN_HAND",
                            Value = new
                            {
                                CardId = cardId,
                                ControllerId = controllerId,
                                LocalPlayer = ParserState.LocalPlayer,
                                OpponentPlayer = ParserState.OpponentPlayer,
                                EntityId = fullEntity.Id,
                                GameState = gameState,
                                AdditionalProps = new {
                                    CreatorCardId = creatorCardId,
                                    IsPremium = fullEntity.GetTag(GameTag.PREMIUM) == 1,
                                }
                            }
                        };
                    },
                    true,
                    creationLogLine) };
        }
    }
}
