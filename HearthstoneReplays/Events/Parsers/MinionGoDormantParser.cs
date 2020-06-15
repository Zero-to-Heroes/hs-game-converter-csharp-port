using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class MinionGoDormantParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public MinionGoDormantParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.DORMANT
                && (node.Object as TagChange).Value == 1;
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
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            if (GameState.CurrentEntities[tagChange.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
                return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "MINION_GO_DORMANT",
                    GameEvent.CreateProvider(
                        "MINION_GO_DORMANT",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        gameState),
                    true,
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
