using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class BlockEndParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BlockEndParser(ParserState ParserState)
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
            return node.Type == typeof(Parser.ReplayData.GameActions.Action);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                "BLOCK_END",
                GameEvent.CreateProvider(
                    "BLOCK_END",
                    null,
                    -1,
                    -1,
                    ParserState,
                    GameState,
                    gameState
                ),
                true,
                node.CreationLogLine
            )};
        }
    }
}
