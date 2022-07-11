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
        private StateFacade StateFacade { get; set; }

        public ReceiveCardInHandParser(ParserState ParserState, StateFacade helper)
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
                && GameState.CurrentEntities.ContainsKey((node.Object as TagChange).Entity)
                && GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) != (int)Zone.DECK;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
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
            var debug = node.Type == typeof(FullEntity) && (node.Object as FullEntity).Id == 68;
            return stateType == StateType.PowerTaskList
                && (appliesToShowEntity || appliesToFullEntity);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, tagChange, null);
            var creator = Oracle.FindCardCreator(GameState, entity, node);
            entity.PlayedWhileInHand.Clear();

            return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "RECEIVE_CARD_IN_HAND",
                    GameEvent.CreateProvider(
                        "RECEIVE_CARD_IN_HAND",
                        cardId,
                        controllerId,
                        entity.Id,
                        StateFacade,
                        gameState,
                        new {
                            CreatorCardId = creator?.Item1, // Used when there is no cardId, so we can show at least the card that created it
                            IsPremium = entity.GetTag(GameTag.PREMIUM) == 1,
                        }),
                    true,
                    node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateEventFromShowEntity(node);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateEventFromFullEntity(node);
            }
            return null;
        }

        private List<GameEventProvider> CreateEventFromShowEntity(Node node)
        {
            ShowEntity showEntity = node.Object as ShowEntity;
            //Logger.Log("Will add creator " + showEntity.GetTag(GameTag.CREATOR) + " //" + showEntity.GetTag(GameTag.DISPLAYED_CREATOR), "");
            var creator = Oracle.FindCardCreatorCardId(GameState, showEntity, node);
            //var creatorEntityId = Oracle.FindCardCreatorEntityId(GameState, showEntity, node);
            var cardId = Oracle.PredictCardId(GameState, creator.Item1, creator.Item2, node, showEntity.CardId);
            var controllerId = showEntity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, showEntity);
            var entity = GameState.CurrentEntities[showEntity.Entity];
            entity.PlayedWhileInHand.Clear();
            var dataNum1 = showEntity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1);
            var dataNum2 = showEntity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_2);
            // Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, showEntity.CardId);
            return new List<GameEventProvider> { GameEventProvider.Create(
                    showEntity.TimeStamp,
                    "RECEIVE_CARD_IN_HAND",
                    GameEvent.CreateProvider(
                        "RECEIVE_CARD_IN_HAND",
                        cardId,
                        controllerId,
                        showEntity.Entity,
                        StateFacade,
                        gameState,
                        new {
                            CreatorCardId = creator?.Item1, // Used when there is no cardId, so we can show at least the card that created it
                            IsPremium = entity.GetTag(GameTag.PREMIUM) == 1 || showEntity.GetTag(GameTag.PREMIUM) == 1,
                            DataNum1 = dataNum1,
                            DataNum2 = dataNum2,
                        }),
                    true,
                    node) };
        }

        private List<GameEventProvider> CreateEventFromFullEntity(Node node)
        {
            FullEntity fullEntity = node.Object as FullEntity;
            var controllerId = fullEntity.GetEffectiveController();
            var previousZone = 0;
            if (GameState.CurrentEntities.ContainsKey(fullEntity.Id))
            {
                previousZone = GameState.CurrentEntities[fullEntity.Id].GetTag(GameTag.ZONE);
                GameState.CurrentEntities[fullEntity.Id].PlayedWhileInHand.Clear();
            }

            // For Nagaling
            int? additionalPlayInfo = null;
            if (fullEntity.GetTag(GameTag.ADDITIONAL_PLAY_REQS_1) != -1)
            {
                additionalPlayInfo = fullEntity.GetTag(GameTag.ADDITIONAL_PLAY_REQS_1);
            }
            var dataNum1 = fullEntity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1);
            var dataNum2 = fullEntity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_2);
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                    fullEntity.TimeStamp,
                    "RECEIVE_CARD_IN_HAND",
                    () => {
                        // We do it here because of Diligent Notetaker - we have to know the last
                        // card played before assigning anything
                        var creator = Oracle.FindCardCreator(GameState, fullEntity, node);
                        var creatorCardId = creator?.Item1;
                        //var creatorEntityId = Oracle.FindCardCreatorEntityId(GameState, fullEntity, node);
                        var cardId = Oracle.PredictCardId(GameState, creatorCardId, creator?.Item2 ?? -1, node, fullEntity.CardId);
                        if (cardId == null && GameState.CurrentTurn <= 1 && fullEntity.GetTag(GameTag.ZONE_POSITION) == 5)
                        {
                            var controller = GameState.GetController(fullEntity.GetEffectiveController());
                            if (controller.GetTag(GameTag.CURRENT_PLAYER) != 1)
                            {
                                cardId = "GAME_005";
                                creatorCardId = "GAME_005";
                            }
                        }
                        var buffingCardEntityCardId = Oracle.GetBuffingCardCardId(creator?.Item2 ?? -1, creatorCardId);
                        var buffCardId = Oracle.GetBuffCardId(creator?.Item2 ?? -1, creatorCardId);
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
                                LocalPlayer = StateFacade.LocalPlayer,
                                OpponentPlayer = StateFacade.OpponentPlayer,
                                EntityId = fullEntity.Id,
                                GameState = gameState,
                                AdditionalProps = new {
                                    CreatorCardId = creatorCardId,
                                    IsPremium = fullEntity.GetTag(GameTag.PREMIUM) == 1,
                                    BuffingEntityCardId = buffingCardEntityCardId,
                                    BuffCardId = buffCardId,
                                    AdditionalPlayInfo = additionalPlayInfo,
                                    DataNum1 = dataNum1,
                                    DataNum2 = dataNum2,
                                }
                            }
                        };
                    },
                    true,
                    node) };
        }
    }
}
