﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class RumbleRunStepParser : ActionParser
    {
        private static readonly int STARTING_HEALTH = 20;

        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public RumbleRunStepParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return false;
            //return node.Type == typeof(TagChange)
            //    && (node.Object as TagChange).Name == (int)GameTag.HEALTH
            //    && !string.IsNullOrWhiteSpace((node.Object as TagChange).DefChange);
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.Action)
                && (node.Object as Parser.ReplayData.GameActions.Action).Data
                    .Where(data => data.GetType() == typeof(TagChange))
                    .Select(data => data as TagChange)
                    .Where(tag => tag.Name == (int)GameTag.HEALTH && !string.IsNullOrWhiteSpace(tag.DefChange))
                    .Count() > 0;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return new List<GameEventProvider> { GameEventProvider.Create(
                (node.Object as Parser.ReplayData.GameActions.Action).TimeStamp,
                "RUMBLE_RUN_STEP",
                () => {
                    if (ParserState.CurrentGame.ScenarioID != (int)Scenario.RUMBLE_RUN)
                    {
                        return null;
                    }
                    var action = (node.Object as Parser.ReplayData.GameActions.Action);
                    var heroEntityId = ParserState.GetEntity(ParserState.LocalPlayer.Id).GetTag(GameTag.HERO_ENTITY);
                    var tagChange = action.Data
                            .Where(data => data.GetType() == typeof(TagChange))
                            .Select(data => data as TagChange)
                            .Where(tag => tag.Name == (int)GameTag.HEALTH && !string.IsNullOrWhiteSpace(tag.DefChange))
                            .Where(tag => tag.Entity == heroEntityId)
                            .FirstOrDefault();
                    // If no tag change for the main player, we use the starting health
                    // The issue with computing things this way is that it's now possible
                    // to emit the event multiple times over the course of a match
                    var healthChangeDef = (tagChange != null ? tagChange.Value : ParserState.GetEntity(heroEntityId).GetTag(GameTag.HEALTH)) - STARTING_HEALTH;
                    var runStep = 1 + healthChangeDef / 5;
                    return new GameEvent
                    {
                        Type = "RUMBLE_RUN_STEP",
                        Value = runStep
                    };
                },
                true,
                node) };
        }
    }
}
