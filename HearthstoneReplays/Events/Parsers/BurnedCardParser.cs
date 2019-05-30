using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class BurnedCardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BurnedCardParser(ParserState ParserState)
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
            return node.Type == typeof(MetaData)
                && (node.Object as MetaData).Meta == (int)MetaDataType.BURNED_CARD;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var meta = node.Object as MetaData;
            List<GameEventProvider> result = new List<GameEventProvider>();
            foreach (var info in meta.MetaInfo)
            {
                var entity = GameState.CurrentEntities[info.Entity];
                var cardId = entity.CardId;
                if (cardId == null)
                {
                    Logger.Log("Could not identify burned card id", info.Entity);
                }
                var controllerId = entity.GetTag(GameTag.CONTROLLER);
                result.Add(GameEventProvider.Create(
                    meta.TimeStamp,
                    () => new GameEvent
                    {
                        Type = "BURNED_CARD",
                        Value = new
                        {
                            CardId = cardId,
                            ControllerId = controllerId,
                            LocalPlayer = ParserState.LocalPlayer,
                            OpponentPlayer = ParserState.OpponentPlayer,
                            EntityId = entity.Id,
                        }
                    },
                    (GameEventProvider provider) =>
                    {
                        var gameEvent = provider.SupplyGameEvent();
                        if (gameEvent.Type != "CARD_REMOVED_FROM_DECK")
                        {
                            return false;
                        }
                        var obj = gameEvent.Value;
                        return obj.GetType().GetProperty("CardId").GetValue(obj, null) as string == cardId;
                    },
                    true,
                    node.CreationLogLine));
            }
            return result;
        }

        // Before 
        public void RemoveDuplicates()
        {

        }
    }
}
