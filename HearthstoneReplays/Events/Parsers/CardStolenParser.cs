using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardStolenParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardStolenParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.CONTROLLER
                && !MindrenderIlluciaParser.IsProcessingMindrenderIlluciaEffect(node, GameState);
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.ShowEntity)
                && GameState.CurrentEntities.ContainsKey((node.Object as ShowEntity).Entity)
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetEffectiveController()
                        != (node.Object as ShowEntity).GetEffectiveController()
                && !MindrenderIlluciaParser.IsProcessingMindrenderIlluciaEffect(node, GameState);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            var lettuceControllerId = entity.GetTag(GameTag.LETTUCE_CONTROLLER);
            if (tagChange.Value == lettuceControllerId)
            {
                return null;
            }

            if (GameState.CurrentEntities[tagChange.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
                return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "CARD_STOLEN",
                    GameEvent.CreateProvider(
                        "CARD_STOLEN",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            newControllerId = tagChange.Value
                        }),
                    true,
                    node) };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var showEntity = node.Object as Parser.ReplayData.GameActions.ShowEntity;
            var cardId = showEntity.CardId;
            var controllerId = GameState.CurrentEntities[showEntity.Entity].GetEffectiveController();
            var lettuceControllerId = showEntity.GetTag(GameTag.LETTUCE_CONTROLLER);
            if (showEntity.GetEffectiveController() == lettuceControllerId)
            {
                return null;
            }

            if (GameState.CurrentEntities[showEntity.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
                return new List<GameEventProvider> { GameEventProvider.Create(
                    showEntity.TimeStamp,
                    "CARD_STOLEN",
                    GameEvent.CreateProvider(
                        "CARD_STOLEN",
                        cardId,
                        controllerId,
                        showEntity.Entity,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            newControllerId = showEntity.GetEffectiveController()
                        }),
                    true,
                    node) };
            }
            return null;
        }
    }
}
