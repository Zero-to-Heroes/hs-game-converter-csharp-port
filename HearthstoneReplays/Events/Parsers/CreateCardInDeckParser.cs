using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

namespace HearthstoneReplays.Events.Parsers
{
    public class CreateCardInDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CreateCardInDeckParser(ParserState ParserState)
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
            // In this case, a FullEntity is created with minimal info, and the real
            // card creation happens in the ShowEntity
            return node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.DECK;
        }

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            var showEntity = node.Object as ShowEntity;
            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            return new GameEventProvider
            {
                Timestamp = DateTimeOffset.Parse(showEntity.TimeStamp),
                SupplyGameEvent = () => new GameEvent
                {
                    Type = "CREATE_CARD_IN_DECK",
                    Value = new
                    {
                        CardId = cardId,
                        ControllerId = controllerId,
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer
                    }
                },
                NeedMetaData = true,
                CreationLogLine = node.CreationLogLine
            };
        }
    }
}
