﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class CounterWillTriggerParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public CounterWillTriggerParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.GameState
                && node.Type == typeof(Parser.ReplayData.GameActions.Action)
                && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.TRIGGER
                && (node.Object as Parser.ReplayData.GameActions.Action).TriggerKeyword == (int)GameTag.COUNTER;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            var entity = GameState.CurrentEntities[action.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            if (GameState.CurrentEntities[action.Entity].GetTag(GameTag.CARDTYPE) == (int)CardType.ENCHANTMENT)
            {
                return null;
            }

            var parentAction = (node.Parent.Object as Parser.ReplayData.GameActions.Action);
            object additionalProps = new { };
            if (parentAction != null && parentAction.Type == (int)BlockType.PLAY)
            {
                additionalProps = new
                {
                    InReactionToCardId = GameState.CurrentEntities[parentAction.Entity]?.CardId,
                    InReactionToEntityId = parentAction.Entity,
                };
            }
            return new List<GameEventProvider> {
                    GameEventProvider.Create(
                        action.TimeStamp,
                        "COUNTER_WILL_TRIGGER",
                        GameEvent.CreateProvider(
                            "COUNTER_WILL_TRIGGER",
                            cardId,
                            controllerId,
                            entity.Id,
                            StateFacade,
                            null,
                            additionalProps),
                       true,
                       node),
                };
        }

        // Typically the case when the opponent plays a quest or a secret
        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
