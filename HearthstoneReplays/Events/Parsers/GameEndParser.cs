using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class GameEndParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public GameEndParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && ((node.Object as TagChange).Name == (int)GameTag.GOLD_REWARD_STATE
                        || ((node.Object as TagChange).Name == (int)GameTag.STATE
                                && (node.Object as TagChange).Value == (int)State.COMPLETE));
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var replayCopy = ParserState.Replay;
            var xmlReplay = new ReplayConverter().xmlFromReplay(replayCopy);
            var gameStateReport = GameState.BuildGameStateReport();
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "GAME_END",
                () => new GameEvent
                {
                    Type = "GAME_END",
                    Value = new
                    {
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer,
                        GameStateReport = gameStateReport,
                        Game = ParserState.CurrentGame,
                        ReplayXml = xmlReplay
                    }
                },
                true,
                node.CreationLogLine) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
