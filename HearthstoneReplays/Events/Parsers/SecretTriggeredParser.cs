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
    public class SecretTriggeredParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public SecretTriggeredParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.Action)
                && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.TRIGGER
                && (node.Object as Parser.ReplayData.GameActions.Action).TriggerKeyword == (int)GameTag.SECRET;
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
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            if (GameState.CurrentEntities[action.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var parentAction = (node.Parent.Object as Parser.ReplayData.GameActions.Action);
                object additionalProps = new { };
                if (parentAction != null && parentAction.Type == (int)BlockType.ATTACK)
                {
                    var proposedAttackerEntityId = parentAction.Data
                        .Where(data => data.GetType() == typeof(TagChange))
                        .Select(data => (TagChange)data)
                        .Where(tag => tag.Name == (int)GameTag.PROPOSED_ATTACKER)
                        .FirstOrDefault()
                        .Value;
                    var proposedAttacker = GameState.CurrentEntities[proposedAttackerEntityId];
                    var proposedDefenderEntityId = parentAction.Data
                        .Where(data => data.GetType() == typeof(TagChange))
                        .Select(data => (TagChange)data)
                        .Where(tag => tag.Name == (int)GameTag.PROPOSED_DEFENDER)
                        .FirstOrDefault()
                        .Value;
                    var proposedDefender = GameState.CurrentEntities[proposedDefenderEntityId];
                    additionalProps = new
                    {
                        ProposedAttackerCardId = proposedAttacker.CardId,
                        ProposedAttackerEntityId = proposedAttackerEntityId,
                        ProposedAttackerControllerId = proposedAttacker.GetTag(GameTag.CONTROLLER),
                        ProposedDefenderCardId = proposedDefender.CardId,
                        ProposedDefenderEntityId = proposedDefenderEntityId,
                        ProposedDefenderControllerId = proposedDefender.GetTag(GameTag.CONTROLLER),
                    };
                }
                var gameState = GameEvent.BuildGameState(ParserState, GameState);
                return new List<GameEventProvider> { GameEventProvider.Create(
                       action.TimeStamp,
                        GameEvent.CreateProvider(
                            "SECRET_TRIGGERED",
                            cardId,
                            controllerId,
                            entity.Id,
                            ParserState,
                            GameState,
                            gameState,
                            additionalProps),
                       true,
                       node.CreationLogLine) };
            }
            return null;
        }

        // Typically the case when the opponent plays a quest or a secret
        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            foreach (var data in action.Data)
            {
                if (data.GetType() == typeof(ShowEntity))
                {
                    var showEntity = data as ShowEntity;
                    if (showEntity.GetTag(GameTag.ZONE) == (int)Zone.SECRET
                        && showEntity.GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
                    {
                        var cardId = showEntity.CardId;
                        var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
                        var gameState = GameEvent.BuildGameState(ParserState, GameState);
                        // For now there can only be one card played per block
                        return new List<GameEventProvider> { GameEventProvider.Create(
                            action.TimeStamp,
                            GameEvent.CreateProvider(
                                "SECRET_PLAYED",
                                cardId,
                                controllerId,
                                showEntity.Entity,
                                ParserState,
                                GameState,
                                gameState),
                            true,
                            node.CreationLogLine) };
                    }
                }
            }
            return null;
        }
    }
}
