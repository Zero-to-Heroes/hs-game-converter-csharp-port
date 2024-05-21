using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

namespace HearthstoneReplays.Events.Parsers
{
    // Like Bombs that explode when you draw them
    public class CardRemovedFromBoardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public CardRemovedFromBoardParser(ParserState ParserState, StateFacade facade)
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
                && ((node.Object as TagChange).Value == (int)Zone.REMOVEDFROMGAME || (node.Object as TagChange).Value == (int)Zone.SETASIDE);
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            if (!GameState.CurrentEntities.ContainsKey(tagChange.Entity))
            {
                Logger.Log("Could not find card to remove from board", node.CreationLogLine);
                return null;
            }
            var entity = GameState.CurrentEntities[tagChange.Entity];
            if (entity.GetTag(GameTag.ZONE) != (int)Zone.PLAY)
            {
                return null;
            }
            if (!entity.IsMinionLike())
            {
                return null;
            }

            Action parentAction = null;
            string removedByCardId = null;
            int? removedByEntityId = null;
            if (node.Parent.Type == typeof(Action))
            {
                parentAction = node.Parent.Object as Action;
                var parentEntity = GameState.CurrentEntities.GetValueOrDefault(parentAction.Entity);
                removedByCardId = parentEntity?.CardId;
                removedByEntityId = parentEntity?.Entity;
            }

            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, tagChange, null);

            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "CARD_REMOVED_FROM_BOARD",
                GameEvent.CreateProvider(
                    "CARD_REMOVED_FROM_BOARD",
                    cardId,
                    controllerId,
                    entity.Id,
                    StateFacade,
                    gameState,
                    new {
                        RemovedByCardId = removedByCardId,
                        RemovedByEntityId = removedByEntityId,
                    }),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
