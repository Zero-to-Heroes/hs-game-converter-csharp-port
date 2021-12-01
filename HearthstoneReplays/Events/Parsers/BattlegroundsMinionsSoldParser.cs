using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using System;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsMinionsSoldParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsMinionsSoldParser(ParserState ParserState)
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
            return (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                    || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                && node.Type == typeof(Action)
                && (node.Object as Action).Type == (int)BlockType.POWER
                && GameState.CurrentEntities.ContainsKey((node.Object as Action).Entity)
                && GameState.CurrentEntities[((node.Object as Action).Entity)].CardId == CardIds.DragToSellBattlegrounds;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Action;
            // Because Sellemental triggers another action, and the zone change is in that new action
            var zoneTagChange = action.GetDataRecursive()
                .Where(data => data is TagChange)
                .Select(data => data as TagChange)
                .Where(tag => tag.Name == (int)GameTag.ZONE && tag.Value == (int)Zone.SETASIDE)
                .FirstOrDefault();
            if (zoneTagChange == null)
            {
                return null;
            }

            var soldMinion = GameState.CurrentEntities[zoneTagChange.Entity];
            var cardId = soldMinion.CardId;
            var controller = soldMinion.GetEffectiveController();
            return new List<GameEventProvider> { GameEventProvider.Create(
                    (node.Object as Action).TimeStamp,
                     "BATTLEGROUNDS_MINION_SOLD",
                    GameEvent.CreateProvider(
                        "BATTLEGROUNDS_MINION_SOLD",
                        cardId,
                        controller,
                        soldMinion.Entity,
                        ParserState,
                        GameState,
                        null),
                    true,
                    node)
                };
        }
    }
}
