using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using System;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsExtraGoldNextTurnParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public BattlegroundsExtraGoldNextTurnParser(ParserState ParserState, StateFacade stateFacade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = stateFacade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            var correctMode = stateType == StateType.PowerTaskList && StateFacade.IsBattlegrounds() && node.Type == typeof(TagChange);
            if (!correctMode)
            {
                return false;
            }

            var tagChange = (node.Object as TagChange);
            var isExtraGoldNextTurn = tagChange.Name == (int)GameTag.TAG_SCRIPT_DATA_NUM_1
                && GameState.CurrentEntities.GetValueOrDefault(tagChange.Entity)?.CardId == CardIds.SouthseaBusker_ExtraGoldNextTurnDntEnchantment;
            var isExtraGoldNextTurnRemoved = tagChange.Name == (int)GameTag.ZONE
                && tagChange.Value == (int)Zone.GRAVEYARD
                && GameState.CurrentEntities.GetValueOrDefault(tagChange.Entity)?.CardId == CardIds.SouthseaBusker_ExtraGoldNextTurnDntEnchantment;
            var isOverconfidence = tagChange.Name == (int)GameTag.ZONE
                //&& tagChange.Value == (int)Zone.PLAY
                && GameState.CurrentEntities.GetValueOrDefault(tagChange.Entity)?.CardId == CardIds.Overconfidence_OverconfidentDntEnchantment_BG28_884e;
            return isExtraGoldNextTurn || isOverconfidence || isExtraGoldNextTurnRemoved;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities.GetValueOrDefault(tagChange.Entity);
            var controllerId = entity.GetController();

            var extraGoldNextTurnValue = entity.CardId == CardIds.SouthseaBusker_ExtraGoldNextTurnDntEnchantment && tagChange.Name == (int)GameTag.TAG_SCRIPT_DATA_NUM_1
                ? tagChange.Value
                : entity.CardId == CardIds.SouthseaBusker_ExtraGoldNextTurnDntEnchantment && tagChange.Name == (int)GameTag.ZONE && tagChange.Value == (int)Zone.GRAVEYARD
                ? 0
                : GameState.CurrentEntities.Values
                    .Where(e => e.CardId == CardIds.SouthseaBusker_ExtraGoldNextTurnDntEnchantment)
                    .Where(e => e.IsInPlay())
                    .FirstOrDefault()
                    ?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1)
                    ?? 0;

            var overconfidentEnchantments = GameState.CurrentEntities.Values
                .Where(e => e.CardId == CardIds.Overconfidence_OverconfidentDntEnchantment_BG28_884e)
                .Where(e => e.IsInPlay(tagChange))
                .ToList();

            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "BATTLEGROUNDS_EXTRA_GOLD_NEXT_TURN",
                GameEvent.CreateProvider(
                "BATTLEGROUNDS_EXTRA_GOLD_NEXT_TURN",
                    null,
                    controllerId,
                    tagChange.Entity,
                    StateFacade,
                    null,
                    new {
                        ExtraGoldNextTurn = extraGoldNextTurnValue,
                        Overconfidences = overconfidentEnchantments.Count(),
                    }),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
