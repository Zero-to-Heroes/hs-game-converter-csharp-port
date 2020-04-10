using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;
using HearthstoneReplays.Parser.ReplayData.Meta;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardRemovedFromDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardRemovedFromDeckParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.SETASIDE
                && GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            var appliesToShowEntity = node.Type == typeof(ShowEntity)
                && ((node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.SETASIDE
                        // Army of the Dead for instance sets the zone to the graveyard
                        || (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.GRAVEYARD)
                && GameState.CurrentEntities.ContainsKey((node.Object as ShowEntity).Entity)
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK;
            var appliesToFullEntity = node.Type == typeof(FullEntity)
                && ((node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.SETASIDE
                        || (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.GRAVEYARD)
                && GameState.CurrentEntities.ContainsKey((node.Object as FullEntity).Id)
                && GameState.CurrentEntities[(node.Object as FullEntity).Id].GetTag(GameTag.ZONE) == (int)Zone.DECK;
            return appliesToShowEntity || appliesToFullEntity;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "CARD_REMOVED_FROM_DECK",
                GameEvent.CreateProvider(
                    "CARD_REMOVED_FROM_DECK",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                        GameState,
                    gameState),
                true,
                node.CreationLogLine) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateEventFromShowEntity(node, node.Object as ShowEntity, node.CreationLogLine);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateEventFromFullEntity(node, node.Object as FullEntity, node.CreationLogLine);
            }
            return null;
        }

        private List<GameEventProvider> CreateEventFromShowEntity(Node node, ShowEntity showEntity, string creationLogLine)
        {
            // Check that this is not a "Burned card", as they both manifest by a ShowEntity
            // with a graveyard zone
            // Usually, the burned card meta data info appears after the showEntity,and is 
            // handled via the duplicatePredicate of the BurnedCard provider, but I'm 
            // keeping this here just in case
            if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                var cardBurned = act.Data
                    .Where((data) => data.GetType() == typeof(MetaData))
                    .Select((data) => data as MetaData)
                    .Where((meta) => meta.Meta == (int)MetaDataType.BURNED_CARD)
                    .Select((meta) => meta.MetaInfo[0])
                    .Any((info) => info.Entity == showEntity.Entity);
                if (cardBurned)
                {
                    return null;
                }
            }

            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                "CARD_REMOVED_FROM_DECK",
                GameEvent.CreateProvider(
                    "CARD_REMOVED_FROM_DECK",
                    cardId,
                    controllerId,
                    showEntity.Entity,
                    ParserState,
                        GameState,
                    gameState),
                true,
                creationLogLine) };
        }

        private List<GameEventProvider> CreateEventFromFullEntity(Node node, FullEntity fullEntity, string creationLogLine)
        {
            if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                var cardBurned = act.Data
                    .Where((data) => data.GetType() == typeof(MetaData))
                    .Select((data) => data as MetaData)
                    .Where((meta) => meta.Meta == (int)MetaDataType.BURNED_CARD)
                    .Select((meta) => meta.MetaInfo[0])
                    .Any((info) => info.Entity == fullEntity.Id);
                if (cardBurned)
                {
                    return null;
                }
            }

            var cardId = fullEntity.CardId;
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                "CARD_REMOVED_FROM_DECK",
                GameEvent.CreateProvider(
                    "CARD_REMOVED_FROM_DECK",
                    cardId,
                    controllerId,
                    fullEntity.Id,
                    ParserState,
                        GameState,
                    gameState),
                true,
                creationLogLine) };
        }
    }
}
