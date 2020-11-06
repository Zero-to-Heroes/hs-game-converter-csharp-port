using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardPresentOnGameStartParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardPresentOnGameStartParser(ParserState ParserState)
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
            return node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.PLAY
                && node.Parent != null && node.Parent.Type == typeof(Game)
                && !ParserState.ReconnectionOngoing;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var cardId = fullEntity.CardId;
            if (fullEntity.GetTag(GameTag.CARDTYPE) != (int)CardType.MINION || fullEntity.GetTag(GameTag.HAS_BEEN_REBORN) == 1)
            {
                return null;
            }
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var startingHealth = fullEntity.GetTag(GameTag.HEALTH);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                "CARD_ON_BOARD_AT_GAME_START",
                GameEvent.CreateProvider(
                    "CARD_ON_BOARD_AT_GAME_START",
                    cardId,
                    controllerId,
                    fullEntity.Id,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        Health = startingHealth
                    }),
                true,
                node) };
        }
    }
}
