using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    // This is used when reconnecting, as cards that have been played are now part of the graveyard right 
    // from the start
    public class CreateCardInGraveyardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CreateCardInGraveyardParser(ParserState ParserState)
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
            //var appliesToShowEntity = node.Type == typeof(ShowEntity)
            //    && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.HAND
            //    && (!GameState.CurrentEntities.ContainsKey((node.Object as ShowEntity).Entity)
            //        || (GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) != (int)Zone.DECK
            //            && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) != (int)Zone.HAND));
            var appliesToFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.GRAVEYARD
                && (node.Object as FullEntity).GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT;
            //return appliesToShowEntity || appliesToFullEntity;
            return appliesToFullEntity;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            //if (node.Type == typeof(ShowEntity))
            //{
            //    return CreateEventFromShowEntity(node);
            //}
            //else 
            if (node.Type == typeof(FullEntity))
            {
                return CreateEventFromFullEntity(node);
            }
            return null;
        }

        //private List<GameEventProvider> CreateEventFromShowEntity(Node node)
        //{
        //    ShowEntity showEntity = node.Object as ShowEntity;
        //    //Logger.Log("Will add creator " + showEntity.GetTag(GameTag.CREATOR) + " //" + showEntity.GetTag(GameTag.DISPLAYED_CREATOR), "");
        //    var creatorCardId = Oracle.FindCardCreatorCardId(GameState, showEntity, node);
        //    var creatorEntityId = Oracle.FindCardCreatorEntityId(GameState, showEntity, node);
        //    var cardId = Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, showEntity.CardId);
        //    var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
        //    var previousZone = GameState.CurrentEntities[showEntity.Entity].GetTag(GameTag.ZONE);
        //    var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
        //    var entity = GameState.CurrentEntities[showEntity.Entity];
        //    // Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, showEntity.CardId);
        //    return new List<GameEventProvider> { GameEventProvider.Create(
        //            showEntity.TimeStamp,
        //            "RECEIVE_CARD_IN_HAND",
        //            GameEvent.CreateProvider(
        //                "RECEIVE_CARD_IN_HAND",
        //                cardId,
        //                controllerId,
        //                showEntity.Entity,
        //                ParserState,
        //                GameState,
        //                gameState,
        //                new {
        //                    CreatorCardId = creatorCardId, // Used when there is no cardId, so we can show at least the card that created it
        //                    IsPremium = entity.GetTag(GameTag.PREMIUM) == 1 || showEntity.GetTag(GameTag.PREMIUM) == 1
        //                }),
        //            true,
        //            node) };
        //}

        private List<GameEventProvider> CreateEventFromFullEntity(Node node)
        {
            FullEntity fullEntity = node.Object as FullEntity;
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                    fullEntity.TimeStamp,
                    "CREATE_CARD_IN_GRAVEYARD",
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
                            Type =  "CREATE_CARD_IN_GRAVEYARD",
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
                    node) };
        }
    }
}
