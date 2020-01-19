using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class DecklistUpdateParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public DecklistUpdateParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return false;
        }

        // We use the action so that the event is emitted after all the "create card in deck" events
        public bool AppliesOnCloseNode(Node node)
        {
            return IsDecklistUpdateAction(node);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            var fullEntities = action.Data
                    .Where(data => data.GetType() == typeof(FullEntity))
                    .Select(data => (FullEntity)data)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.HERO_DECK_ID) > 0)
                    .ToList();
            // Time the "new deck" action right after all the "create card in deck" ones
            var timestamp = action.Data
                    .Where(data => data.GetType() == typeof(FullEntity))
                    .Select(data => (FullEntity)data)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.DECK)
                    .ToList()
                    .Last()
                    .TimeStamp;
            return fullEntities.Select(fullEntity =>
                {
                    var deckId = fullEntity.GetTag(GameTag.HERO_DECK_ID);
                    var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
                    return GameEventProvider.Create(
                        timestamp,
                        GameEvent.CreateProvider(
                            "DECKLIST_UPDATE",
                            null,
                            controllerId,
                            -1,
                            ParserState,
                            GameState,
                            null,
                            new
                            {
                                DeckId = deckId,
                            }),
                        true,
                        node.CreationLogLine);
                })
                .ToList();
        }

        private bool IsDecklistUpdateAction(Node node)
        {
            if (node.Type != typeof(Parser.ReplayData.GameActions.Action))
            {
                return false;
            }
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            return action.Data != null
                && (action.Data
                    .Where(data => data.GetType() == typeof(FullEntity))
                    .Select(data => (FullEntity)data)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.HERO_DECK_ID) > 0)
                    .ToList()
                    .Count > 0);
        }
    }
}
