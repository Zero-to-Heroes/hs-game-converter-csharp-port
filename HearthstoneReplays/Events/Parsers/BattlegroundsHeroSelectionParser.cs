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
    public class BattlegroundsHeroSelectionParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsHeroSelectionParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.MULLIGAN_STATE
                && (node.Object as TagChange).Value == (int)Mulligan.INPUT;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var playerId = ParserState.LocalPlayer.PlayerId;
            var fullEntities = GameState.CurrentEntities.Values
                .Where(data => data.GetTag(GameTag.CONTROLLER) == playerId)
                .Where(data => data.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(data => data.GetTag(GameTag.ZONE) == (int)Zone.HAND)
                .Where(data => data.GetTag(GameTag.BACON_HERO_CAN_BE_DRAFTED) == 1)
                .ToList();
            fullEntities.Sort((a, b) => a.GetTag(GameTag.ZONE_POSITION) - b.GetTag(GameTag.ZONE_POSITION));
            if (fullEntities.Count > 0)
            {
                return new List<GameEventProvider> { GameEventProvider.Create(
                   tagChange.TimeStamp,
                   () => new GameEvent
                   {
                       Type = "BATTLEGROUNDS_HERO_SELECTION",
                       Value = new
                       {
                           CardIds = fullEntities.Select(entity => entity.CardId).ToList()
                       }
                   },
                   false,
                   node.CreationLogLine
                )};
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
