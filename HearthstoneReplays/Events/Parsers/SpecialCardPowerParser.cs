using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

namespace HearthstoneReplays.Events.Parsers
{
    public class SpecialCardPowerParser : ActionParser
    {
        private static IList<string> SPECIAL_POWER_CARDS = new List<string>()
        {
            CardIds.LorekeeperPolkelt,
        };

        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public SpecialCardPowerParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            Action action = null;
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(Action)
                && (action = node.Object as Action).Type == (int)BlockType.POWER
                && GameState.CurrentEntities.ContainsKey(action.Entity)
                && SPECIAL_POWER_CARDS.Contains(GameState.CurrentEntities[action.Entity].CardId);
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var action = node.Object as Action;
            var entity = GameState.CurrentEntities[action.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, null);
            return new List<GameEventProvider> {
                GameEventProvider.Create(
                    action.TimeStamp,
                    "SPECIAL_CARD_POWER_TRIGGERED",
                    GameEvent.CreateProvider(
                        "SPECIAL_CARD_POWER_TRIGGERED",
                        cardId,
                        controllerId,
                        entity.Id,
                        StateFacade,
                        gameState),
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
