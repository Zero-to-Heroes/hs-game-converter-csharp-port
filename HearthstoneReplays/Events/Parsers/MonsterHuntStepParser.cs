using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class MonsterRunStepParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public MonsterRunStepParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.HEALTH
                && !string.IsNullOrWhiteSpace((node.Object as TagChange).DefChange);
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var runStep = 1 + (tagChange.Value - 10) / 5;
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "MONSTER_HUNT_STEP",
                () => {
                    if (ParserState.CurrentGame.ScenarioID != (int)Scenario.MONSTER_HUNT)
                    {
                        return null;
                    }
                    var heroEntityId = ParserState.GetEntity(ParserState.LocalPlayer.Id).GetTag(GameTag.HERO_ENTITY);
                    if (tagChange.Entity != heroEntityId)
                    {
                        return null;
                    }
                    return new GameEvent
                    {
                        Type = "MONSTER_HUNT_STEP",
                        Value = runStep
                    };
                },
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
