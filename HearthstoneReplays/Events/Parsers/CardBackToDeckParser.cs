using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardBackToDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardBackToDeckParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.DECK;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var zoneInt = entity.GetTag(GameTag.ZONE) == -1 ? 0 : entity.GetTag(GameTag.ZONE);
            var initialZone = ((Zone)zoneInt).ToString();
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            if (cardId == null || cardId.Length == 0)
            {
                var creatorCardId = Oracle.FindCardCreatorCardId(GameState, entity, node);
                var creatorEntityId = Oracle.FindCardCreatorEntityId(GameState, entity, node);
                cardId = Oracle.PredictCardId(GameState, creatorCardId, creatorEntityId, node, cardId);
            }

            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                zoneInt == (int)Zone.SETASIDE ? "CREATE_CARD_IN_DECK" : "CARD_BACK_TO_DECK",
                GameEvent.CreateProvider(
                    zoneInt == (int)Zone.SETASIDE ? "CREATE_CARD_IN_DECK" : "CARD_BACK_TO_DECK",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        InitialZone = initialZone,
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
