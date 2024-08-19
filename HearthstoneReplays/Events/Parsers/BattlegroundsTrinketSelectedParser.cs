using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using System;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Parser.ReplayData;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsTrinketSelectedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public BattlegroundsTrinketSelectedParser(ParserState ParserState, StateFacade helper)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = helper;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.GameState
                && StateFacade.IsBattlegrounds()
                && node.Type == typeof(Choice)
                && ParserState.CurrentChosenEntites != null;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var choice = node.Object as Choice;
            var chosenEntity = GameState.CurrentEntities[choice.Entity];
            if (chosenEntity == null || chosenEntity.GetTag(GameTag.CARDTYPE) != (int)CardType.BATTLEGROUND_TRINKET)
            {
                return null;
            }

            var controllerId = chosenEntity.GetEffectiveController();

            return new List<GameEventProvider> { GameEventProvider.Create(
                choice.TimeStamp,
                "BATTLEGROUNDS_TRINKET_SELECTED",
                () => {
                    return new GameEvent
                    {
                        Type = "BATTLEGROUNDS_TRINKET_SELECTED",
                        Value = new
                        {
                            CardId = chosenEntity.CardId,
                            LocalPlayer = StateFacade.LocalPlayer,
                            OpponentPlayer = StateFacade.OpponentPlayer,
                        }
                    };
                },
                true,
                node)
            };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}