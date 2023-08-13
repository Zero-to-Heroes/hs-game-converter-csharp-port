using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsPlayerLeaderboardPlaceUpdatedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public BattlegroundsPlayerLeaderboardPlaceUpdatedParser(ParserState ParserState, StateFacade stateFacade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = stateFacade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && StateFacade.IsBattlegrounds()
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.PLAYER_LEADERBOARD_PLACE;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var hero = GameState.CurrentEntities[tagChange.Entity];
            if (tagChange.Value > 0 && hero?.CardId != null && hero.CardId != BartenderBob)
            {
                return new List<GameEventProvider> {  GameEventProvider.Create(
               tagChange.TimeStamp,
               "BATTLEGROUNDS_LEADERBOARD_PLACE",
               () => {
                    if (!StateFacade.IsBattlegrounds())
                    {
                        return null;
                    }
                    return new GameEvent
                    {
                        Type = "BATTLEGROUNDS_LEADERBOARD_PLACE",
                        Value = new
                        {
                            CardId = hero.CardId,
                            LeaderboardPlace = tagChange.Value,
                        }
                    };
               },
               true,
               node) };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
