using HearthstoneReplays.Parser;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using HearthstoneReplays.Parser.ReplayData.GameActions;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsDuoTeammatePlayerBoardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public BattlegroundsDuoTeammatePlayerBoardParser(ParserState ParserState, StateFacade helper)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = helper;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return false;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            var debug = node.Type == typeof(SubSpell)
                    && (node.Object as SubSpell).Prefab.StartsWith("ReuseFX_Generic_OverrideSpawn_FromPortal_Super_Random_SuppressPlaySounds");
            var subSpell = debug ? node.Object as SubSpell : null;
            return stateType == StateType.GameState 
                    && StateFacade.IsBattlegrounds()
                    && GameState.GetGameEntity()?.GetTag(GameTag.BOARD_VISUAL_STATE) == 2
                    && node.Type == typeof(SubSpell)
                    && (node.Object as SubSpell).Prefab.StartsWith("ReuseFX_Generic_OverrideSpawn_FromPortal_Super_Random_SuppressPlaySounds");
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        // TODO: SUB_SPELL_START - SpellPrefabGUID=ReuseFX_Generic_OverrideSpawn_FromPortal_Super_Random_SuppressPlaySounds.prefab:239cad74b85aed548987a7909afc293c
        // indicates that we swap players (we still need to check that we are still in combat probably, and that
        // happens after the current node)
        // Within that spell change, we have the new board, so it should be possible to
        // send an array of boards, for each side
        // However, it might be difficult, since the GS is already fully elapsed when we're building the boards here
        // so getting the initial value of each minion on the board might prove too difficult
        // TODO: add a "FUTURE_BOARD" event in GS that will be used to append the info
        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var subSpell = node.Object as SubSpell;
            var opponent = StateFacade.OpponentPlayer;
            var player = StateFacade.LocalPlayer;

            // The info here will be degraded, since we aren't able to look into the future (we're already using the
            // GameState), but it should be good enough
            var opponentBoard = BattlegroundsPlayerBoardParser.CreateProviderFromAction(opponent, true, player, GameState, StateFacade);
            var playerBoard = BattlegroundsPlayerBoardParser.CreateProviderFromAction(player, false, player, GameState, StateFacade);


            // If we move back to GS logs, this probably needs to be in another parser
            GameState.BgsHasSentNextOpponent = false;

            var result = new List<GameEventProvider>();
            result.Add(GameEventProvider.Create(
                   subSpell.Timestamp,
                   "BATTLEGROUNDS_DUO_FUTURE_TEAMMATE_BOARD",
                   () => new GameEvent
                   {
                       Type = "BATTLEGROUNDS_DUO_FUTURE_TEAMMATE_BOARD",
                       Value = new
                       {
                           Timestamp = subSpell.Timestamp,
                           PlayerBoard = playerBoard,
                           OpponentBoard = opponentBoard,
                       }
                   },
                   true,
                   node
               ));
            return result;
        }
    }
}

