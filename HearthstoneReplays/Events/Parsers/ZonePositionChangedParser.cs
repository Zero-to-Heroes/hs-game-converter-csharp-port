﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class ZonePositionChangedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public ZonePositionChangedParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            TagChange tagChange;
            return stateType == StateType.PowerTaskList
                // Limit it to merceanries, the only mode where this is used, to limit the impact on the number of events sent (esp. in BG)
                && ParserState.IsMercenaries()
                && node.Type == typeof(TagChange)
                && ((tagChange = node.Object as TagChange).Name == (int)GameTag.ZONE_POSITION);
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            var zonePosition = tagChange.Value;
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "ZONE_POSITION_CHANGED",
                GameEvent.CreateProvider(
                    "ZONE_POSITION_CHANGED",
                    cardId,
                    controllerId,
                    entity.Id,
                    StateFacade,
                    null,
                    new {
                        ZonePosition = zonePosition,
                    }
                ),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
