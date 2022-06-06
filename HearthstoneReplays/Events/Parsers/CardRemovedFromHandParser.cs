using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    // Like Bombs that explode when you draw them
    public class CardRemovedFromHandParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public CardRemovedFromHandParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.SETASIDE;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var zoneInt = entity.GetTag(GameTag.ZONE) == -1 ? 0 : entity.GetTag(GameTag.ZONE);
            if (zoneInt != (int)Zone.HAND)
            {
                return null;
            }

            var isEcho = (entity.GetTag(GameTag.NON_KEYWORD_ECHO) == 1 || entity.GetTag(GameTag.ECHO) == 1) && entity.GetTag(GameTag.GHOSTLY) == 1;
            if (isEcho)
            {
                return null;
            }

            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, tagChange, null);
            entity.PlayedWhileInHand.Clear();
            if (entity.IsImmolateDiscard())
            {
                cardId = null;
            }
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "CARD_REMOVED_FROM_HAND",
                GameEvent.CreateProvider(
                    "CARD_REMOVED_FROM_HAND",
                    cardId,
                    controllerId,
                    entity.Id,
                    StateFacade,
                    gameState),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
