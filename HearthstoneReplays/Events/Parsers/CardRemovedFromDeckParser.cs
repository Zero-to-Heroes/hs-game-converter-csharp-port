﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

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
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.SETASIDE
                && GameState.CurrentEntities.ContainsKey((node.Object as ShowEntity).Entity)
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK;
            var appliesToFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.SETASIDE
                && GameState.CurrentEntities.ContainsKey((node.Object as FullEntity).Id)
                && GameState.CurrentEntities[(node.Object as FullEntity).Id].GetTag(GameTag.ZONE) == (int)Zone.DECK;
            return appliesToShowEntity || appliesToFullEntity;
        }

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            return GameEventProvider.Create(
                tagChange.TimeStamp,
                () => new GameEvent
                {
                    Type = "CARD_REMOVED_FROM_DECK",
                    Value = new
                    {
                        CardId = cardId,
                        ControllerId = controllerId,
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer
                    }
                },
                true,
                node.CreationLogLine);
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateEventFromShowEntity(node.Object as ShowEntity, node.CreationLogLine);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateEventFromFullEntity(node.Object as FullEntity, node.CreationLogLine);
            }
            return null;
        }

        private GameEventProvider CreateEventFromShowEntity(ShowEntity showEntity, string creationLogLine)
        {
            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            return GameEventProvider.Create(
                showEntity.TimeStamp,
                () => new GameEvent
                {
                    Type = "CARD_REMOVED_FROM_DECK",
                    Value = new
                    {
                        CardId = cardId,
                        ControllerId = controllerId,
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer
                    }
                },
                true,
                creationLogLine);
        }

        private GameEventProvider CreateEventFromFullEntity(FullEntity fullEntity, string creationLogLine)
        {
            var cardId = fullEntity.CardId;
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            return GameEventProvider.Create(
                fullEntity.TimeStamp,
                () => new GameEvent
                {
                    Type = "CARD_REMOVED_FROM_DECK",
                    Value = new
                    {
                        CardId = cardId,
                        ControllerId = controllerId,
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer
                    }
                },
                true,
                creationLogLine);
        }
    }
}