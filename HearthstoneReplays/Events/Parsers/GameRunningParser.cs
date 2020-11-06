using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    // Used to notify when the STATE switches to RUNNING, which signifies the initial deck and board population has been done
    public class GameRunningParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public GameRunningParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.STATE
                && (node.Object as TagChange).Value == (int)State.RUNNING;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            // We don't want to wait to get the current state, as it's possible that some cards have 
            // already been drawn by then
            var stateCopy = ParserState.GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.DECK)
                    .Select(entity => entity.GetTag(GameTag.CONTROLLER))
                    .ToList();
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "GAME_RUNNING",
                () => {
                    var playerDeckCount = stateCopy
                            .Where(entity => entity == ParserState.LocalPlayer.PlayerId)
                            .ToList()
                            .Count();
                    var opponentDeckCount = stateCopy
                            .Where(entity => entity == ParserState.OpponentPlayer.PlayerId)
                            .ToList()
                            .Count();
                    return new GameEvent
                        {
                            Type = "GAME_RUNNING",
                            Value = new
                            {
                                LocalPlayer = ParserState.LocalPlayer,
                                OpponentPlayer = ParserState.OpponentPlayer,
                                AdditionalProps = new
                                {
                                    PlayerDeckCount = playerDeckCount,
                                    OpponentDeckCount = opponentDeckCount,
                                }
                            }
                        };
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
