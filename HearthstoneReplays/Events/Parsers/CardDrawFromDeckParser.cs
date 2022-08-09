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
        private static List<string> SHOULD_USE_ADVANCED_PREDICTION_FOR_CARD_DRAW = new List<string>() { CardIds.SuspiciousAlchemist_AMysteryEnchantment };
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public CardDrawFromDeckParser(ParserState ParserState, StateFacade helper)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = helper;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.HAND
                && (GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK
                    || GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == -1);
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            var appliesToShowEntity = node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.HAND
                && GameState.CurrentEntities.ContainsKey((node.Object as ShowEntity).Entity)
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK;
            var appliesToFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.HAND
                && GameState.CurrentEntities.ContainsKey((node.Object as FullEntity).Id)
                && GameState.CurrentEntities[(node.Object as FullEntity).Id].GetTag(GameTag.ZONE) == (int)Zone.DECK;
            return stateType == StateType.PowerTaskList
                && (appliesToShowEntity || appliesToFullEntity);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            // When a card is sent back to the deck using tradeable, we know its card ID
            // We can't simply remove the card ID when the card is sent back to the deck, because this would lead to
            // a desynch between HS's game state and our own game state, which can then cause further problems down 
            // the line that can be hard to debug
            // So we need to know, here, if the cardId should be public or not
            // About using "REVEALED": if a card is set to REVEALED = 0, then drawn in a context where we should know what 
            // it is (eg for your own cards), we don't want to hide the info
            // About using "IS_USING_TRADE


            var controllerId = entity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, tagChange, null);
            // If we compute this when triggering the event, we will get a "gift" icon because the 
            // card is already in hand
            var wasInDeck = entity.GetTag(GameTag.ZONE) == (int)Zone.DECK;
            // Because Encumbered Pack Mule reveals itself if drawn during mulligan, we need to 
            // have a special rule
            var isBeforeMulligan = GameState.GetGameEntity().GetTag(GameTag.NEXT_STEP) == -1;
            if (isBeforeMulligan && cardId == CardIds.EncumberedPackMule)
            {
                return null;
            }

            var dataTag1 = entity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1, 0);
            // Cost is needed for cards like Lady Prestor, which use the cost of the drawn card to remove the right card from the deck
            var cost = entity.GetCost();
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "CARD_DRAW_FROM_DECK",
                () => {
                    var entityId = tagChange.Entity;
                    // We do it here because of Keymaster Alabaster - we need to know the last card
                    // that has been drawn
                    // TODO: this was true when relying on the GS logs. Now that we use PTL, maybe we can move this back? Or maybe we 
                    // need the full BLOCK to be complete first?
                    var creator = wasInDeck ? null : Oracle.FindCardCreator(GameState, entity, node, false);
                    // Always return this info, and the client has a list of public card creators they are allowed to show
                    var lastInfluencedByCard = Oracle.FindCardCreator(GameState, entity, node);
                    var lastInfluencedByCardId = lastInfluencedByCard?.Item1;
                    var predictedCardId = Oracle.PredictCardId(GameState, creator?.Item1, -1, node, cardId);
                    // Issue: if a card creates a card and draws one, this will flag them both (while the draw could be something else)
                    // This was introduced to flag the cards created by the Suspicious* cards
                    if (SHOULD_USE_ADVANCED_PREDICTION_FOR_CARD_DRAW.Contains(lastInfluencedByCardId))
                    {
                        predictedCardId = predictedCardId ?? Oracle.PredictCardId(GameState, lastInfluencedByCardId, lastInfluencedByCard?.Item2 ?? -1, node, cardId);
                    }
                    GameState.OnCardDrawn(entity.Entity);
                    var finalCardId = cardId != null && cardId.Length > 0 ? cardId : predictedCardId;
                    return new GameEvent
                    {
                        Type =  "CARD_DRAW_FROM_DECK",
                        Value = new
                        {
                            CardId = finalCardId,
                            ControllerId = controllerId,
                            LocalPlayer = StateFacade.LocalPlayer,
                            OpponentPlayer = StateFacade.OpponentPlayer,
                            EntityId = entity.Id,
                            GameState = gameState,
                            AdditionalProps = new {
                                IsPremium = entity.GetTag(GameTag.PREMIUM) == 1,
                                CreatorCardId = creator?.Item1,
                                LastInfluencedByCardId = lastInfluencedByCardId,
                                DataTag1 = dataTag1,
                                Cost = cost,
                            }
                        }
                    };
                },
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateEventFromShowEntity(node.Object as ShowEntity, node);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateEventFromFullEntity(node.Object as FullEntity, node);
            }
            return null;
        }

        private List<GameEventProvider> CreateEventFromShowEntity(ShowEntity showEntity, Node node)
        {
            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetEffectiveController();
            var entity = GameState.CurrentEntities[showEntity.Entity];
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, showEntity);
            var wasInDeck = entity.GetTag(GameTag.ZONE) == (int)Zone.DECK;
            var isBeforeMulligan = GameState.GetGameEntity().GetTag(GameTag.NEXT_STEP) == -1;

            var dataTag1 = entity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1, 0);
            var cost = showEntity.GetCost();
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                "CARD_DRAW_FROM_DECK",
                () => {
                    // Because Encumbered Pack Mule reveals itself if drawn during mulligan, we need to 
                    // have a special rule to hide it when the opponent draws it
                    if (isBeforeMulligan && cardId == CardIds.EncumberedPackMule && controllerId != StateFacade.LocalPlayer.PlayerId)
                    {
                        return null;
                    }
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
                            LocalPlayer = StateFacade.LocalPlayer,
                            OpponentPlayer = StateFacade.OpponentPlayer,
                            EntityId = showEntity.Entity,
                            GameState = gameState,
                            AdditionalProps = new {
                                IsPremium = entity.GetTag(GameTag.PREMIUM) == 1 || showEntity.GetTag(GameTag.PREMIUM) == 1,
                                CreatorCardId = creatorCardId,
                                LastInfluencedByCardId = lastInfluencedByCardId?.Item1,
                                DataTag1 = dataTag1,
                                Cost = cost,
                            }
                        }
                    };
                },
                true,
                node) };
        }

        private List<GameEventProvider> CreateEventFromFullEntity(FullEntity fullEntity, Node node)
        {
            var cardId = fullEntity.CardId;
            var controllerId = fullEntity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, null);
            var wasInDeck = fullEntity.GetTag(GameTag.ZONE) == (int)Zone.DECK;
            // Because Encumbered Pack Mule reveals itself if drawn during mulligan, we need to 
            // have a special rule
            var isBeforeMulligan = GameState.GetGameEntity().GetTag(GameTag.NEXT_STEP) == -1;
            if (isBeforeMulligan && cardId == CardIds.EncumberedPackMule)
            {
                cardId = "";
            }

            var dataTag1 = fullEntity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1, 0);
            var cost = fullEntity.GetCost();
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                "CARD_DRAW_FROM_DECK",
                () => {
                    // We do it here because of Keymaster Alabaster - we need to know the last card
                    // that has been drawn
                    var creator = wasInDeck ? null : Oracle.FindCardCreator(GameState, fullEntity, node, false);
                    var lastInfluencedByCardId = Oracle.FindCardCreator(GameState, fullEntity, node)?.Item1;
                    GameState.OnCardDrawn(fullEntity.Entity);
                    return new GameEvent
                    {
                        Type =  "CARD_DRAW_FROM_DECK",
                        Value = new
                        {
                            CardId = cardId,
                            ControllerId = controllerId,
                            LocalPlayer = StateFacade.LocalPlayer,
                            OpponentPlayer = StateFacade.OpponentPlayer,
                            EntityId = fullEntity.Entity,
                            GameState = gameState,
                            AdditionalProps = new {
                                IsPremium = fullEntity.GetTag(GameTag.PREMIUM) == 1 || fullEntity.GetTag(GameTag.PREMIUM) == 1,
                                CreatorCardId = creator?.Item1,
                                LastInfluencedByCardId = lastInfluencedByCardId,
                                DataTag1 = dataTag1,
                                Cost = cost,
                            }
                        }
                    };
                },
                true,
                node) };
        }
    }
}
