using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using static HearthstoneReplays.Events.CardIds.Collectible;
using HearthstoneReplays.Parser.ReplayData.Meta;

namespace HearthstoneReplays.Events.Parsers
{
    public class InitialCardInDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public InitialCardInDeckParser(ParserState ParserState)
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
            var appliesOnFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.DECK
                // We want only the cards created when the game starts
                && node.Parent.Type == typeof(GameAction);
            return appliesOnFullEntity;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                GameEvent.CreateProvider(
                    "INITIAL_CARD_IN_DECK",
                    null,
                    controllerId,
                    fullEntity.Id,
                    ParserState,
                    GameState,
                    null),
                true,
                node.CreationLogLine) };
        }
    }
}
