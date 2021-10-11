using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class NumCardsDrawnThisTurnParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public NumCardsDrawnThisTurnParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.NUM_CARDS_DRAWN_THIS_TURN;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardsDrawn = tagChange.Value;
            var controllerId = entity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "NUM_CARDS_DRAWN_THIS_TURN",
                GameEvent.CreateProvider(
                    "NUM_CARDS_DRAWN_THIS_TURN",
                    null,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        NumCardsDrawn = cardsDrawn,
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
