using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class HealthDefChangeParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public HealthDefChangeParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.HEALTH
                && !string.IsNullOrWhiteSpace((node.Object as TagChange).DefChange);

        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var defHealthChange = tagChange.Value;
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "HEALTH_DEF_CHANGED",
                GameEvent.CreateProvider(
                    "HEALTH_DEF_CHANGED",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    null,
                    new {
                        NewHealth = defHealthChange,
                    }),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
