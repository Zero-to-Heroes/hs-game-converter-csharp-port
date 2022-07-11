using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

namespace HearthstoneReplays.Events.Parsers
{
    public class StartOfGameTriggerParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public StartOfGameTriggerParser(ParserState ParserState, StateFacade helper)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = helper;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(Action)
                && (node.Object as Action).Type == (int)BlockType.TRIGGER
                && (node.Object as Action).TriggerKeyword == (int)GameTag.START_OF_GAME;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var action = node.Object as Action;
            var actionEntity = GameState.CurrentEntities[action.Entity];
            var controllerId = actionEntity?.GetTag(GameTag.CONTROLLER) ?? -1;
            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                "START_OF_GAME",
                GameEvent.CreateProvider(
                    "START_OF_GAME",
                    actionEntity?.CardId,
                    controllerId,
                    action.Entity,
                    StateFacade,
                    null),
                true,
                node) 
            };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
