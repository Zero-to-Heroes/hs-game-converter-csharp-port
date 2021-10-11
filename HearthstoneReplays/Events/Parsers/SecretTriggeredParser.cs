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
            var controllerId = entity.GetEffectiveController();
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
                        ProposedAttackerControllerId = proposedAttacker.GetEffectiveController(),
                        ProposedDefenderCardId = proposedDefender.CardId,
                        ProposedDefenderEntityId = proposedDefenderEntityId,
                        ProposedDefenderControllerId = proposedDefender.GetEffectiveController(),
                    };
                }
                else if (parentAction != null && parentAction.Type == (int)BlockType.PLAY)
                {
                    additionalProps = new
                    {
                        InReactionToCardId = GameState.CurrentEntities[parentAction.Entity]?.CardId,
                        InReactionToEntityId = parentAction.Entity,
                    };
                }
                var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
                return new List<GameEventProvider> {
                    GameEventProvider.Create(
                        action.TimeStamp,
                        "SECRET_WILL_TRIGGER",
                        GameEvent.CreateProvider(
                            "SECRET_WILL_TRIGGER",
                            cardId,
                            controllerId,
                            entity.Id,
                            ParserState,
                            GameState,
                            gameState,
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
                        "SECRET_TRIGGERED",
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
                       node) 
                };
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
                        var controllerId = showEntity.GetEffectiveController();
                        var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
                        // For now there can only be one card played per block
                        return new List<GameEventProvider> { GameEventProvider.Create(
                            action.TimeStamp,
                            "SECRET_PLAYED",
                            GameEvent.CreateProvider(
                                "SECRET_PLAYED",
                                cardId,
                                controllerId,
                                showEntity.Entity,
                                ParserState,
                                GameState,
                                gameState),
                            true,
                            node) };
                    }
                }
            }
            return null;
        }
    }
}
