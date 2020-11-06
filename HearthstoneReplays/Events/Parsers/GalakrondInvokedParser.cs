using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class GalakrondInvokedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public GalakrondInvokedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.INVOKE_COUNTER;
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
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "GALAKROND_INVOKED",
                    GameEvent.CreateProvider(
                        "GALAKROND_INVOKED",
                        null,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        gameState,
                        new
                        {
                            TotalInvoke = tagChange.Value,
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
