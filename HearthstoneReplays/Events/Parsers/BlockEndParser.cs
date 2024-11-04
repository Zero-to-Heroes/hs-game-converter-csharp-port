using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class BlockEndParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public BlockEndParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return false;
            return stateType == StateType.PowerTaskList
                && !StateFacade.IsBattlegrounds()
                && node.Type == typeof(TagChange) 
                // Don't remember where this is coming from...
                && (node.Object as TagChange).Name == (int)GameTag.ATTACKABLE_BY_RUSH;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
            return stateType == StateType.PowerTaskList
                && !StateFacade.IsBattlegrounds()
                && node.Type == typeof(Parser.ReplayData.GameActions.Action);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var element = node.Object as TagChange;
            //var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, element, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                element.TimeStamp,
                "GAME_STATE_UPDATE",
                GameEvent.CreateProvider(
                    "GAME_STATE_UPDATE",
                    null,
                    -1,
                    -1,
                    StateFacade
                    //gameState
                ),
                true,
                node
            )};
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var element = node.Object as Parser.ReplayData.GameActions.Action;
            //var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, null);
            var lastElement = element.Data.Count > 0 ? element.Data[element.Data.Count - 1] : element;
            return new List<GameEventProvider> { GameEventProvider.Create(
                lastElement.TimeStamp,
                "GAME_STATE_UPDATE",
                GameEvent.CreateProvider(
                    "GAME_STATE_UPDATE",
                    null,
                    -1,
                    -1,
                    StateFacade
                    //gameState
                ),
                true,
                node
            )};
        }
    }
}
