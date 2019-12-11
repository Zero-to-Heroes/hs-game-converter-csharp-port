using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsPlayerBoardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsPlayerBoardParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.NEXT_STEP
                && (node.Object as TagChange).Value == (int)Step.MAIN_START_TRIGGERS;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var opponent = ParserState.OpponentPlayer;
            var player = ParserState.LocalPlayer;
            return new List<GameEventProvider> { CreateProvider(node, player), CreateProvider(node, opponent) };
        }

        private GameEventProvider CreateProvider(Node node, Player player)
        {
            var tagChange = node.Object as TagChange;
            var heroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .ToList();
            var hero = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                .FirstOrDefault();
            //Logger.Log("Hero " + hero.CardId, hero.Entity);
            if (hero?.CardId != null 
                && hero.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl 
                && hero.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2)
            {
                // We don't use the game state builder here because we really need the full entities
                var board = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
                    .Select(entity => entity.Clone())
                    .ToList();
                //Logger.Log("board has " + board.Count + " entities", "");
                return GameEventProvider.Create(
                   tagChange.TimeStamp,
                   () => new GameEvent
                   {
                       Type = "BATTLEGROUNDS_PLAYER_BOARD",
                       Value = new
                       {
                           Hero = hero,
                           CardId = hero.CardId,
                           Board = board,
                       }
                   },
                   false,
                   node.CreationLogLine);
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
