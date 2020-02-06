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
            var attackerId = action.Data
                .Where(data => data.GetType() == typeof(TagChange))
                .Select(data => (TagChange)data)
                // ATTACKING is set once the attack goes through, otherwise it's PROPOSED_ATTACKER
                .Where(tagChange => tagChange.Name == (int)GameTag.ATTACKING && tagChange.Value == 1)
                .FirstOrDefault() // Might use the last non-zero, in case of secrets? To be checked
                .Entity;
            var defenderId = action.Data
                .Where(data => data.GetType() == typeof(TagChange))
                .Select(data => (TagChange)data)
                .Where(tagChange => tagChange.Name == (int)GameTag.DEFENDING && tagChange.Value == 1)
                .FirstOrDefault()
                .Entity;
            if (!GameState.CurrentEntities.ContainsKey(attackerId) || !GameState.CurrentEntities.ContainsKey(defenderId))
            {
                Logger.Log("Could not find entity or target", attackerId + " // " + defenderId + " // " + node.CreationLogLine);
            }
            var attacker = GameState.CurrentEntities[attackerId];
            var defender = GameState.CurrentEntities[defenderId];
            var eventType = "ATTACKING_UNKNOWN";
            if (defender.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
            {
                eventType = "ATTACING_MINION";
            }
            else if (defender.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
            {
                eventType = "ATTACKING_HERO";
            }
            var attackerCardId = attacker.CardId;
            var defenderCardId = defender.CardId;
            var attackerControllerId = attacker.GetTag(GameTag.CONTROLLER);
            var defenderControllerId = defender.GetTag(GameTag.CONTROLLER);
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
                        AttackerCardId = attackerCardId,
                        AttackerEntityId = attacker.Id,
                        AttackerControllerId = attackerControllerId,
                        DefenderCardId = defenderCardId,
                        DefenderEntityId = defender.Id,
                        DefenderControllerId = defenderControllerId,
                    }),
                true,
                node.CreationLogLine)
            };
        }
    }
}
