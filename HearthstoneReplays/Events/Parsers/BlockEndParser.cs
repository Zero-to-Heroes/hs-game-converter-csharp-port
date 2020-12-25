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

        public BlockEndParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS
                && ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS_FRIENDLY
                && node.Type == typeof(TagChange) 
                && (node.Object as TagChange).Name == (int)GameTag.ATTACKABLE_BY_RUSH;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS
                && ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS_FRIENDLY
                && node.Type == typeof(Parser.ReplayData.GameActions.Action);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var element = node.Object as TagChange;
            var gameState = GameEvent.BuildGameState(ParserState, GameState, element, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                element.TimeStamp,
                "GAME_STATE_UPDATE",
                GameEvent.CreateProvider(
                    "GAME_STATE_UPDATE",
                    null,
                    -1,
                    -1,
                    ParserState,
                    GameState,
                    gameState
                ),
                true,
                node
            )};
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var element = node.Object as Parser.ReplayData.GameActions.Action;
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                element.TimeStamp,
                "GAME_STATE_UPDATE",
                GameEvent.CreateProvider(
                    "GAME_STATE_UPDATE",
                    null,
                    -1,
                    -1,
                    ParserState,
                    GameState,
                    gameState
                ),
                true,
                node
            )};
        }
    }
}
