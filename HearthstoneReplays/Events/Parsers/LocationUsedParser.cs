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
    public class LocationUsedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public LocationUsedParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            Action action = null;
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(Action)
                && (action = node.Object as Action).Type == (int)BlockType.POWER
                && GameState.CurrentEntities.GetValueOrDefault(action.Entity)?.GetCardType() == (int)CardType.LOCATION;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var action = node.Object as Action;
            var entity = GameState.CurrentEntities.GetValueOrDefault(action.Entity);
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                 "LOCATION_USED",
                GameEvent.CreateProvider(
                    "LOCATION_USED",
                    cardId,
                    controllerId,
                    entity.Id,
                    StateFacade,
                    null),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
