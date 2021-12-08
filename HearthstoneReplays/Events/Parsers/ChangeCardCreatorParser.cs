using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class ChangeCardCreatorParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public ChangeCardCreatorParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.DISPLAYED_CREATOR;
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

            var cardId = string.IsNullOrEmpty(entity.CardId) ? null : entity.CardId;
            var creatorCardId = GameState.CurrentEntities.ContainsKey(tagChange.Value) ? GameState.CurrentEntities[tagChange.Value]?.CardId : null;
            var controllerId = entity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "CARD_CREATOR_CHANGED",
                    GameEvent.CreateProvider(
                        "CARD_CREATOR_CHANGED",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            CreatorCardId = creatorCardId,
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
