using HearthstoneReplays.Parser;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class MercenariesAbilityActivatedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public MercenariesAbilityActivatedParser(ParserState ParserState)
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
            Action action = null;
            return node.Type == typeof(Action)
                && (action = node.Object as Action).Type == (int)BlockType.PLAY
                && GameState.CurrentEntities.ContainsKey(action.Entity)
                && GameState.CurrentEntities[action.Entity].GetZone() == (int)Zone.LETTUCE_ABILITY
                && GameState.CurrentEntities[action.Entity].GetCardType() == (int)CardType.LETTUCE_ABILITY;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Action;
            var entity = GameState.CurrentEntities[action.Entity];
            var controllerId = entity.GetEffectiveController();
            var cardId = entity.CardId;
            var abilityOwnerEntityId = entity.GetTag(GameTag.LETTUCE_ABILITY_OWNER);
            var isTreasure = entity.GetTag(GameTag.LETTUCE_IS_TREASURE_CARD) == 1;
            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                "MERCENARIES_ABILITY_ACTIVATED",
                GameEvent.CreateProvider(
                    "MERCENARIES_ABILITY_ACTIVATED",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    null,
                    new
                    {
                        AbilityOwnerEntityId = abilityOwnerEntityId,
                        IsTreasure = isTreasure,
                    }
                ),
                true,
                node) };
        }
    }
}
