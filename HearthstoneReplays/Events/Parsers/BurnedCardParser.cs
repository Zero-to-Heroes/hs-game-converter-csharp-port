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
            if (meta == null)
            {
                Logger.Log("Could not find meta info", "");
            }
            List<GameEventProvider> result = new List<GameEventProvider>();
            foreach (var info in meta.MetaInfo)
            {
                var entity = GameState.CurrentEntities[info.Entity];
                if (entity == null)
                {
                    Logger.Log("Could not find entity", info.Entity);
                }
                var cardId = entity.CardId;
                if (cardId == null)
                {
                    Logger.Log("Could not identify burned card id", info.Entity);
                }
                var controllerId = entity.GetTag(GameTag.CONTROLLER);
                var gameState = GameEvent.BuildGameState(ParserState, GameState);
                result.Add(GameEventProvider.Create(
                    meta.TimeStamp,
                    GameEvent.CreateProvider(
                        "BURNED_CARD",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        gameState),
                    (GameEventProvider provider) =>
                    {
                        var gameEvent = provider.SupplyGameEvent();
                        if (gameEvent == null)
                        {
                            Logger.Log("Could not identify gameEvent", provider.CreationLogLine);
                        }
                        if (gameEvent.Type != "CARD_REMOVED_FROM_DECK")
                        {
                            return false;
                        }
                        dynamic obj = gameEvent.Value;
                        return obj != null && (obj.CardId as string) == cardId;
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
