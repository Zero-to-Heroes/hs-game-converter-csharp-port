using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class CounterTriggerParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CounterTriggerParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.Action)
                && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.TRIGGER
                && (node.Object as Parser.ReplayData.GameActions.Action).TriggerKeyword == (int)GameTag.COUNTER;
        }

        public bool AppliesOnCloseNode(Node node)
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
                            ParserState,
                            GameState,
                            null,
                            additionalProps),
                       true,
                       node,
                       true,
                       false,
                       // We short-circuit so that the app knows that a secret will trigger, and can take action accordingly
                       // (esp. if the secret is Counterspell or Oh My Yogg)
                       true),
                    GameEventProvider.Create(
                        action.TimeStamp,
                        "COUNTER_TRIGGERED",
                        GameEvent.CreateProvider(
                            "COUNTER_TRIGGERED",
                            cardId,
                            controllerId,
                            entity.Id,
                            ParserState,
                            GameState,
                            null,
                            additionalProps),
                       true,
                       node)
                };
        }

        // Typically the case when the opponent plays a quest or a secret
        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
