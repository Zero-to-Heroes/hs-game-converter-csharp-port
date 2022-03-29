using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HearthstoneReplays.Events
{
    public class GameStateShort
    {
        public int ActivePlayerId { get; set; }
        public GameStateShortPlayer Player { get; set; }
        public GameStateShortPlayer Opponent { get; set; }
    }

    public class GameStateShortPlayer
    {
        public GameStateShortSmallEntity Hero { get; set; }
        public GameStateShortSmallEntity Weapon { get; set; }
        public List<GameStateShortSmallEntity> Hand { get; set; }
        public List<GameStateShortSmallEntity> Board { get; set; }
        public List<GameStateShortSmallEntity> Deck { get; set; }
        public List<GameStateShortSmallEntity> LettuceAbilities { get; set; }
    }

    public class GameStateShortSmallEntity
    {
        public int entityId { get; set; }
        public string cardId { get; set; }
        public int attack { get; set; }
        public int health { get; set; }
        public List<Tag> tags { get; set; }
        public List<GameStateShortEnchantment> enchantments { get; set; }
    }

    public class GameStateShortEnchantment
    {
        public int entityId { get; set; }
        public string cardId { get; set; }
        public List<Tag> tags { get; set; }

    }
}
