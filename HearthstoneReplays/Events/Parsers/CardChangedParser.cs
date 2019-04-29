using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardChangedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardChangedParser(ParserState ParserState)
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
            return node.Type == typeof(ChangeEntity);
        }
        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var changeEntity = node.Object as ChangeEntity;
            var eventName = "CARD_CHANGED";
            if (GameState.CurrentEntities[changeEntity.Entity].GetTag(GameTag.ZONE) == (int)Zone.PLAY)
            {
                eventName = "CARD_CHANGED_ON_BOARD";
            }
            else if (GameState.CurrentEntities[changeEntity.Entity].GetTag(GameTag.ZONE) == (int)Zone.HAND)
            {
                eventName = "CARD_CHANGED_IN_HAND";
            }
            var cardId = changeEntity.CardId;
            var entity = GameState.CurrentEntities[changeEntity.Entity];
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            return new List<GameEventProvider> { GameEventProvider.Create(
                changeEntity.TimeStamp,
                () => new GameEvent
                {
                    Type = eventName,
                    Value = new
                    {
                        CardId = cardId,
                        ControllerId = controllerId,
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer,
                        EntityId = entity.Id,
                    }
                },
                true,
                node.CreationLogLine) };
        }
    }
}
