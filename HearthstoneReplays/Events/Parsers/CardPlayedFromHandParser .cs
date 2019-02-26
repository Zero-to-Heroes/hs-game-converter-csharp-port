using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardPlayedFromHandParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardPlayedFromHandParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.PLAY
                && GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == (int)Zone.HAND;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.Action) 
                && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.PLAY;
        }

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            if (GameState.CurrentEntities[tagChange.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                return new GameEventProvider
                {
                    Timestamp = DateTimeOffset.Parse(tagChange.TimeStamp),
                    SupplyGameEvent = () => new GameEvent
                    {
                        Type = "CARD_PLAYED",
                        Value = new
                        {
                            CardId = cardId,
                            ControllerId = controllerId,
                            LocalPlayer = ParserState.LocalPlayer,
                            OpponentPlayer = ParserState.OpponentPlayer
                        }
                    },
                    NeedMetaData = false
                };
            }
            return null;
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            foreach (var data in action.Data) {
                if (data.GetType() == typeof(ShowEntity))
                {
                    var showEntity = data as ShowEntity;
                    if (showEntity.GetTag(GameTag.ZONE) == (int)Zone.PLAY 
                        && showEntity.GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
                    {
                        var cardId = showEntity.CardId;
                        var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
                        // For now there can only be one card played per block
                        return new GameEventProvider
                        {
                            Timestamp = DateTimeOffset.Parse(action.TimeStamp),
                            SupplyGameEvent = () => new GameEvent
                            {
                                Type = "CARD_PLAYED",
                                Value = new
                                {
                                    CardId = cardId,
                                    ControllerId = controllerId,
                                    LocalPlayer = ParserState.LocalPlayer,
                                    OpponentPlayer = ParserState.OpponentPlayer
                                }
                            },
                            NeedMetaData = false
                        };
                    }
                }
            }
            return null;
        }
    }
}
