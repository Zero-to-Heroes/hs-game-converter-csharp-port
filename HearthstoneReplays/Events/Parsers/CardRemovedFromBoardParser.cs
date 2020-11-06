using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    // Like Bombs that explode when you draw them
    public class CardRemovedFromBoardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardRemovedFromBoardParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && ((node.Object as TagChange).Value == (int)Zone.REMOVEDFROMGAME || (node.Object as TagChange).Value == (int)Zone.SETASIDE);
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            if (!GameState.CurrentEntities.ContainsKey(tagChange.Entity))
            {
                Logger.Log("Could not find card to remove from board", node.CreationLogLine);
                return null;
            }
            var entity = GameState.CurrentEntities[tagChange.Entity];
            if (entity.GetTag(GameTag.ZONE) != (int)Zone.PLAY)
            {
                return null;
            }
            if (entity.GetTag(GameTag.CARDTYPE) != (int)CardType.MINION)
            {
                return null;
            }

            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "CARD_REMOVED_FROM_BOARD",
                GameEvent.CreateProvider(
                    "CARD_REMOVED_FROM_BOARD",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    gameState),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
