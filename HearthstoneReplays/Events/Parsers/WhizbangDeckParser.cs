﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class WhizbangDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public WhizbangDeckParser(ParserState ParserState)
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
            return node.Type == typeof(PlayerEntity);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var playerEntity = node.Object as PlayerEntity;
            var whizbangDeckId = playerEntity.GetTag(GameTag.WHIZBANG_DECK_ID);
            if (whizbangDeckId == -1)
            {
                return null;
            }

            return new List<GameEventProvider> { GameEventProvider.Create(
                playerEntity.TimeStamp,
                "WHIZBANG_DECK_ID",
                () => {
                    // The info is also logged for the opponent, but we ignore it
                    if (playerEntity.PlayerId != ParserState.LocalPlayer.PlayerId)
                    {
                        return null;
                    }

                    //Logger.Log("Providing game event for WinnerParser", node.CreationLogLine);
                    return new GameEvent
                    {
                        Type = "WHIZBANG_DECK_ID",
                        Value = new
                        {
                            DeckId = whizbangDeckId,
                        }
                    };
                },
                true,
                node) };
        }
    }
}
