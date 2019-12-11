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
            return false;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                && node.Type == typeof(Action)
                && (node.Object as Action).Type == (int)BlockType.TRIGGER
                && (node.Object as Action).Entity == GameState.GetGameEntity().Id
                && (node.Object as Action).Data.Where(data => data is FullEntity).Count() >= 2;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Action;
            var playerId = ParserState.LocalPlayer.PlayerId;
            var fullEntities = action.Data
                .Where(data => data is FullEntity)
                .Select(data => data as FullEntity)
                .Where(data => data.GetTag(GameTag.CONTROLLER) == playerId)
                .Where(data => data.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(data => data.GetTag(GameTag.ZONE) == (int)Zone.HAND)
                .Where(data => data.GetTag(GameTag.BACON_HERO_CAN_BE_DRAFTED) == 1)
                .ToList();
            fullEntities.Sort((a, b) => a.GetTag(GameTag.ZONE_POSITION) - b.GetTag(GameTag.ZONE_POSITION));
            if (fullEntities.Count > 0)
            {
                return new List<GameEventProvider> { GameEventProvider.Create(
                   action.TimeStamp,
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
    }
}
