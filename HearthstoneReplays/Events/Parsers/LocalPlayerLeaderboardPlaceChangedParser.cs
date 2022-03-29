using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class LocalPlayerLeaderboardPlaceChangedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public LocalPlayerLeaderboardPlaceChangedParser(ParserState ParserState, StateFacade helper)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = helper;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
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
            FullEntity entity = GameState.CurrentEntities.TryGetValue(tagChange.Entity, out entity) ? entity : null;
            if (entity == null)
            {
                return null;
            }
            if (entity.GetEffectiveController() == StateFacade.LocalPlayer.PlayerId)
            {
                return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "LOCAL_PLAYER_LEADERBOARD_PLACE_CHANGED",
                    GameEvent.CreateProvider(
                        "LOCAL_PLAYER_LEADERBOARD_PLACE_CHANGED",
                        null,
                        -1,
                        entity.Id,
                        StateFacade,
                        null,
                        new {
                            NewPlace = tagChange.Value
                        }),
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
