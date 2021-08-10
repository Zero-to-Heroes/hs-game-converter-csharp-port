using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;

namespace HearthstoneReplays.Events.Parsers
{
    public class LinkedEntityParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public LinkedEntityParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.LINKED_ENTITY;
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
            var linkedEntity = GameState.CurrentEntities[tagChange.Value];
            // In the case of Spy-o-matic, the active player discovers a card, and so the new entities are 
            // attached to the active player. We in fact want to have the controllerId be the controller 
            // of the original card, so that we can properly transfer information to it
            // However, the card copy (CARD_REVEALED) is usually attached to the same player as the one 
            // from the LINKED_ENTITY, so we have to keep the same controller.
            // We'll add the LinkedEntityControllerId attribute instead to make the link
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "LINKED_ENTITY",
                GameEvent.CreateProvider(
                    "LINKED_ENTITY",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    gameState, 
                    new {
                        LinkedEntityId = tagChange.Value,
                        LinkedEntityControllerId = linkedEntity.GetTag(GameTag.CONTROLLER),
                        LinkedEntityZone = linkedEntity.GetZone(),
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
