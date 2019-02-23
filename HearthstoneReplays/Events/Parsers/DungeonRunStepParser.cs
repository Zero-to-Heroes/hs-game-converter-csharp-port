using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

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

        public bool NeedMetaData()
        {
            return true;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.HEALTH
                && !string.IsNullOrWhiteSpace((node.Object as TagChange).DefChange)
                && ParserState.CurrentGame.ScenarioID == (int)Scenario.DUNGEON_RUN;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var runStep = 1 + (tagChange.Value - 15) / 5;
            var heroEntityId = ParserState.GetTag(
                ParserState.GetEntity(ParserState.LocalPlayer.Id).Tags,
                GameTag.HERO_ENTITY);
            if (tagChange.Entity == heroEntityId)
            {
                return new GameEventProvider
                {
                    Timestamp = DateTimeOffset.Parse(tagChange.TimeStamp),
                    GameEvent = new GameEvent
                    {
                        Type = "DUNGEON_RUN_STEP",
                        Value = runStep
                    }
                };
            }
            return null;
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
