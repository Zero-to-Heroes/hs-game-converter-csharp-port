using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardDrawFromDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardDrawFromDeckParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool NeedMetaData()
        {
            return true;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.HAND
                && (GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK 
                    || GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == -1);
        }

        public bool AppliesOnCloseNode(Node node)
        {
            var appliesToShowEntity = node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.HAND
                && (GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) == -1 
                    || GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK);
            var appliesToFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.HAND
                && (GameState.CurrentEntities[(node.Object as FullEntity).Id].GetTag(GameTag.ZONE) == -1
                    || GameState.CurrentEntities[(node.Object as FullEntity).Id].GetTag(GameTag.ZONE) == (int)Zone.DECK);
            return appliesToShowEntity || appliesToFullEntity;
        }

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            return new GameEventProvider
            {
                Timestamp = DateTimeOffset.Parse(tagChange.TimeStamp),
                GameEvent = new GameEvent
                {
                    Type = "CARD_DRAW_FROM_DECK",
                    Value = new
                    {
                        CardId = entity.CardId,
                        ControllerId = entity.GetTag(GameTag.CONTROLLER),
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer
                    }
                }
            };
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateEventFromShowEntity(node.Object as ShowEntity);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateEventFromFullEntity(node.Object as FullEntity);
            }
            return null;
        }

        private GameEventProvider CreateEventFromShowEntity(ShowEntity showEntity)
        {
            return new GameEventProvider
            {
                Timestamp = DateTimeOffset.Parse(showEntity.TimeStamp),
                GameEvent = new GameEvent
                {
                    Type = "CARD_DRAW_FROM_DECK",
                    Value = new
                    {
                        CardId = showEntity.CardId,
                        ControllerId = showEntity.GetTag(GameTag.CONTROLLER),
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer
                    }
                }
            };
        }

        private GameEventProvider CreateEventFromFullEntity(FullEntity fullEntity)
        {
            return new GameEventProvider
            {
                Timestamp = DateTimeOffset.Parse(fullEntity.TimeStamp),
                GameEvent = new GameEvent
                {
                    Type = "CARD_DRAW_FROM_DECK",
                    Value = new
                    {
                        CardId = fullEntity.CardId,
                        ControllerId = fullEntity.GetTag(GameTag.CONTROLLER),
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer
                    }
                }
            };
        }
    }
}
