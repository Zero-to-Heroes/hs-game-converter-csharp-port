using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class DungeonRunStepParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public DungeonRunStepParser(ParserState ParserState)
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
            var runStep = 1 + (tagChange.Value - 15) / 5;
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                () => {
                    if (ParserState.CurrentGame.ScenarioID != (int)Scenario.DUNGEON_RUN)
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
                        Type = "DUNGEON_RUN_STEP",
                        Value = runStep
                    };
                },
                true,
                node.CreationLogLine) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
