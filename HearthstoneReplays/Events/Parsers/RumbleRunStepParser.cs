using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

namespace HearthstoneReplays.Events.Parsers
{
    public class RumbleRunStepParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public RumbleRunStepParser(ParserState ParserState)
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

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange; 
            // The player starts with 20 Health, and gains an additional 5 Health per defeated boss, 
            // up to 45 Health for the eighth, and final boss.
            int runStep = 1 + (tagChange.Value - 20) / 5;
            return new GameEventProvider
            {
                Timestamp = DateTimeOffset.Parse(tagChange.TimeStamp),
                SupplyGameEvent = () => {
                    if (ParserState.CurrentGame.ScenarioID != (int)Scenario.RUMBLE_RUN)
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
                        Type = "RUMBLE_RUN_STEP",
                        Value = runStep
                    };
                },
                NeedMetaData = true,
                CreationLogLine = node.CreationLogLine
            };
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
