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
    public class BattlegroundsTriplesCountUpdatedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsTriplesCountUpdatedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                    || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.PLAYER_TRIPLES;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var hero = GameState.CurrentEntities[tagChange.Entity];
            // The value is set to 0 when rotating the entities it seems
            if (hero?.CardId != null && hero.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl && tagChange.Value > 0)
            {
                return new List<GameEventProvider> {  GameEventProvider.Create(
               tagChange.TimeStamp,
                "BATTLEGROUNDS_TRIPLE",
               () => new GameEvent
               {
                   Type = "BATTLEGROUNDS_TRIPLE",
                   Value = new
                   {
                       CardId = hero.CardId,
                   }
               },
               false,
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
