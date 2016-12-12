#region

using System;
using System.Collections.Generic;
using System.Linq;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Parser.ReplayData.Meta.Options;
using HearthstoneReplays.Parser.ReplayData.Entities;

#endregion

namespace HearthstoneReplays.Parser
{
	public class ParserState
	{
		public ParserState()
		{
			FirstPlayerId = -1;
			Reset();
		}

		public HearthstoneReplay Replay { get; set; }
		public Game CurrentGame { get; set; }
		public Node Node { get; set; }
		public GameData GameData { get; set; }
		public SendChoices SendChoices { get; set; }
		public Choices Choices { get; set; }
		public Options Options { get; set; }
		public Option CurrentOption { get; set; }
		public object LastOption { get; set; }
		public int FirstPlayerId { get; set; }
	    public int CurrentPlayerId { get; set; }
		public ChosenEntities CurrentChosenEntites { get; set; }

		public void Reset()
		{
			Replay = new HearthstoneReplay();
			CurrentGame = new Game();
		}

		public void UpdateCurrentNode(params Type[] types)
		{
			while(Node.Parent != null && types.All(x => x != Node.Type))
				Node = Node.Parent;
		}

		public List<PlayerEntity> getPlayers()
		{
			List<PlayerEntity> players = new List<PlayerEntity>();
			foreach (GameData x in CurrentGame.Data)
			{
				if (x is PlayerEntity) players.Add((PlayerEntity)x);
			}
			return players;
		}
	}
}