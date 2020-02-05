using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class AttackParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public AttackParser(ParserState ParserState)
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
            return node.Type == typeof(Parser.ReplayData.GameActions.Action)
                && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.ATTACK;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {

            var action = node.Object as Parser.ReplayData.GameActions.Action;
            //if (action.Entity)
            var proposedAttacker = action.Data
                .Where(data => data.GetType() == typeof(TagChange))
                .Select(data => (TagChange)data)
                .Where(tagChange => tagChange.Name == (int)GameTag.PROPOSED_ATTACKER)
                .FirstOrDefault() // Might use the last non-zero, in case of secrets? To be checked
                .Value;
            var proposedDefender = action.Data
                .Where(data => data.GetType() == typeof(TagChange))
                .Select(data => (TagChange)data)
                .Where(tagChange => tagChange.Name == (int)GameTag.PROPOSED_DEFENDER)
                .FirstOrDefault()
                .Value;
            if (!GameState.CurrentEntities.ContainsKey(proposedAttacker) || !GameState.CurrentEntities.ContainsKey(proposedDefender))
            {
                Logger.Log("Could not find entity or target", action.Entity + " // " + action.Target + " // " + node.CreationLogLine);
            }
            var source = GameState.CurrentEntities[proposedAttacker];
            var target = GameState.CurrentEntities[proposedDefender];
            var eventType = "ATTACK_ON_UNKNOWN";
            if (target.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
            {
                eventType = "ATTACK_ON_MINION";
            }
            else if (target.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
            {
                eventType = "ATTACK_ON_HERO";
            }
            var sourceCardId = source.CardId;
            var targetCardId = target.CardId;
            var sourceControllerId = source.GetTag(GameTag.CONTROLLER);
            var targetControllerId = target.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                GameEvent.CreateProvider(
                    eventType,
                    null,
                    -1,
                    -1,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        SourceCardId = sourceCardId,
                        SourceEntityId = source.Id,
                        SourceControllerId = sourceControllerId,
                        TargetCardId = targetCardId,
                        TargetEntityId = target.Id,
                        TargetControllerId = targetControllerId,
                    }),
                true,
                node.CreationLogLine)
            };
        }
    }
}
