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
    public class CardBuffedInHandParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardBuffedInHandParser(ParserState ParserState)
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
            var isPower = node.Type == typeof(Parser.ReplayData.GameActions.Action)
                 && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.POWER;
            var isTrigger = node.Type == typeof(Parser.ReplayData.GameActions.Action)
                 && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.TRIGGER;
            return isPower || isTrigger;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            return CreateEventProviderForAction(node, node.CreationLogLine);
        }

        private List<GameEventProvider> CreateEventProviderForAction(Node node, string creationLogLine)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            var actionEntity = GameState.CurrentEntities[action.Entity];
            var entitiesBuffedInHand = action.Data
                .Where(data => data.GetType() == typeof(MetaData))
                .Select(data => data as MetaData)
                .Where(meta => meta.Meta == (int)MetaDataType.TARGET)
                .SelectMany(meta => meta.MetaInfo)
                .Select(info => GameState.CurrentEntities[info.Entity])
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.HAND)
                .ToList();
            if (entitiesBuffedInHand.Count == 0)
            {
                return null;
            }
            var controllerId = actionEntity.GetTag(GameTag.CONTROLLER);
            var result = entitiesBuffedInHand
                .Select(entity =>
                {
                    return GameEventProvider.Create(
                        action.TimeStamp,
                        "CARD_BUFFED_IN_HAND",
                        GameEvent.CreateProvider(
                            "CARD_BUFFED_IN_HAND",
                            entity.CardId,
                            entity.GetTag(GameTag.CONTROLLER),
                            entity.Entity,
                            ParserState,
                            GameState,
                            null,
                            new
                            {
                                BuffingEntityCardId = actionEntity.CardId,
                            }),
                        true,
                        creationLogLine);
                })
                .ToList();
            return result;
        }
    }
}
