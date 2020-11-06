using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using System;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsStartOfCombatParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsStartOfCombatParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                    || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.BOARD_VISUAL_STATE
                && (node.Object as TagChange).Value == 2;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            return new List<GameEventProvider> {  
                GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "BATTLEGROUNDS_COMBAT_START",
                    () => new GameEvent
                    {
                        Type = "BATTLEGROUNDS_COMBAT_START"
                    },
                    false,
                    node) 
            };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
