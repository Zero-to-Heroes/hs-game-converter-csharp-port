using HearthstoneReplays.Parser;
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
        private StateFacade Helper { get; set; }

        public WhizbangDeckParser(ParserState ParserState, StateFacade helper)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.Helper = helper;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return false;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(PlayerEntity);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var playerEntity = node.Object as PlayerEntity;
            // Old tag, which we don't want to show for the opponent
            var whizbangDeckId = playerEntity.PlayerId == Helper.OpponentPlayer.PlayerId 
                ? -1 
                : playerEntity.GetTag(GameTag.WHIZBANG_DECK_ID);
            if (whizbangDeckId == -1)
            {
                // In this case, this gives the card ID (like TOY_700t2)
                var splendiferousCardId = playerEntity.GetTag(GameTag.WHIZBANG_SPLENDIFEROUS_DECK_ID);
                if (splendiferousCardId != -1)
                {
                    whizbangDeckId = GetSplendiferousDeckId(splendiferousCardId);
                }
            }
            if (whizbangDeckId == -1)
            {
                return null;
            }

            return new List<GameEventProvider> { GameEventProvider.Create(
                playerEntity.TimeStamp,
                "WHIZBANG_DECK_ID",
                GameEvent.CreateProvider(
                    "WHIZBANG_DECK_ID",
                    null,
                    playerEntity.GetEffectiveController(),
                    playerEntity.Id,
                    Helper,
                    null,
                    new {
                        DeckId = whizbangDeckId,
                    }),
                true,
                node) };
        }

        private int GetSplendiferousDeckId(int splendiferousCardId)
        {
            switch (splendiferousCardId)
            {
                // Manually map a card id with a deck from the DECK.json DBF
                case 106243: return 5342; // Priest
                case 106244: return 5343; // Death Knight Rainbow
                case 106245: return 5345; // Rogue
                case 106246: return 5344; // Warlock
                //case 106247: return ; // Copycat
                case 106248: return 5385; // Mage
                case 106249: return 5346; // Druid
                case 106252: return 5381; // Paladin
                case 106253: return 5382; // Hunter
                case 106251: return 5383; // DH
                case 106250: return 5384; // Shaman
                case 108932: return 5410; // Warrior
                //case 106244: return 5317; // DK_DH
                //case 106244: return 5318; // Rogue_Hunter
                //case 106244: return 5319; // Paladin_DK
                //case 106244: return 5336; // ??? d4
                //case 106244: return 5320; // Warlock_Priest
                //case 106244: return 5337; // ??? d5
                //case 106244: return 5321; // Druid_Warrior
                //case 106244: return 5322; // Shaman_Mage
                //case 106244: return 5327; // ??? d1
                //case 106244: return 5328; // ??? d2
                //case 106244: return 5329; // ??? d3
                //case 106244: return 5322; // Shaman_Mage
                default: return -1;
            }
        }
    }
}
