using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class ZoneChangeParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public ZoneChangeParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            // Limit it to merceanries, the only mode where this is used, to limit the impact on the number of events sent (esp. in BG)
            return ParserState.IsMercenaries()
                && node.Type == typeof(TagChange)
                && ((node.Object as TagChange).Name == (int)GameTag.ZONE);
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            var zone = tagChange.Value;
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "ZONE_CHANGED",
                GameEvent.CreateProvider(
                    "ZONE_CHANGED",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    null,
                    new {
                        Zone = zone,
                    }
                ),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
