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

        public LocalPlayerLeaderboardPlaceChangedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.PLAYER_LEADERBOARD_PLACE;
        }

        public bool AppliesOnCloseNode(Node node)
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
            if (entity.GetTag(GameTag.CONTROLLER) == ParserState.LocalPlayer.PlayerId)
            {
                return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "LOCAL_PLAYER_LEADERBOARD_PLACE_CHANGED",
                    GameEvent.CreateProvider(
                        "LOCAL_PLAYER_LEADERBOARD_PLACE_CHANGED",
                        null,
                        -1,
                        entity.Id,
                        ParserState,
                        GameState,
                        null,
                        new {
                            NewPlace = tagChange.Value
                        }),
                    true,
                    node.CreationLogLine) };

            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
