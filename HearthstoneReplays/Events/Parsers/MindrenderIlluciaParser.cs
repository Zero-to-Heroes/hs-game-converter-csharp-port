﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;

namespace HearthstoneReplays.Events.Parsers
{
    public class MindrenderIlluciaParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public MindrenderIlluciaParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(Action)
                && (node.Object as Action).Type == (int)BlockType.POWER
                && GameState.CurrentEntities.ContainsKey((node.Object as Action).Entity)
                && GameState.CurrentEntities[((node.Object as Action).Entity)].CardId == Collectible.Priest.MindrenderIllucia;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(Action)
                && (node.Object as Action).Type == (int)BlockType.TRIGGER
                && GameState.CurrentEntities.ContainsKey((node.Object as Action).Entity)
                && GameState.CurrentEntities[((node.Object as Action).Entity)].CardId == NonCollectible.Priest.MindrenderIllucia_MindSwapEnchantment;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var action = node.Object as Action;
            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                "MINDRENDER_ILLUCIA_START",
                GameEvent.CreateProvider(
                    "MINDRENDER_ILLUCIA_START",
                    null,
                    -1,
                    -1,
                    ParserState,
                    GameState,
                    null),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Action;
            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                "MINDRENDER_ILLUCIA_END",
                GameEvent.CreateProvider(
                    "MINDRENDER_ILLUCIA_END",
                    null,
                    -1,
                    -1,
                    ParserState,
                    GameState,
                    null),
                true,
                node) };
        }

        public static bool IsProcessingMindrenderIlluciaEffect(Node node, GameState gameState)
        {
            if (node.Parent == null || node.Parent.Type != typeof(Action))
            {
                return false;
            }

            var parentAction = node.Parent.Object as Action;
            return
                // Effect start
                (parentAction.Type == (int)BlockType.POWER
                    && gameState.CurrentEntities.ContainsKey(parentAction.Entity)
                    && gameState.CurrentEntities[parentAction.Entity].CardId == Collectible.Priest.MindrenderIllucia)
                || (parentAction.Type == (int)BlockType.TRIGGER
                    && gameState.CurrentEntities.ContainsKey(parentAction.Entity)
                    && gameState.CurrentEntities[parentAction.Entity].CardId == NonCollectible.Priest.MindrenderIllucia_MindSwapEnchantment);
        }
    }
}
