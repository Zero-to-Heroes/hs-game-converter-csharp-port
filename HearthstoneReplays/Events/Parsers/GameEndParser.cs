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
        private StateFacade StateFacade { get; set; }

        public GameEndParser(ParserState ParserState, StateFacade helper)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = helper;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(TagChange)
                && ((node.Object as TagChange).Name == (int)GameTag.GOLD_REWARD_STATE
                        || ((node.Object as TagChange).Name == (int)GameTag.STATE
                                && (node.Object as TagChange).Value == (int)State.COMPLETE));
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var replayCopy = StateFacade.GSReplay;
            // Update the name info
            foreach (var player in ParserState.getPlayers())
            {
                var gsPlayer = StateFacade.GetPlayers().Find(p => p.Id == player.Id);
                player.Name = gsPlayer?.Name ?? player.Name;
            }
            var xmlReplay = new ReplayConverter().xmlFromReplay(replayCopy);
            var gameStateReport = GameState.BuildGameStateReport(StateFacade);
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, tagChange, null);
            Logger.Log("Enqueuing GAME_END event", "");
            ParserState.EndCurrentGame();
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "GAME_END",
                () => new GameEvent
                {
                    Type = "GAME_END",
                    Value = new
                    {
                        LocalPlayer = StateFacade.LocalPlayer,
                        OpponentPlayer = StateFacade.OpponentPlayer,
                        GameStateReport = gameStateReport,
                        Game = ParserState.CurrentGame,
                        ReplayXml = xmlReplay,
                        Spectating = ParserState.Spectating,
                    }
                },
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
