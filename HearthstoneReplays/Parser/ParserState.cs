#region

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using HearthstoneReplays.Events;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Parser.ReplayData.Meta.Options;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Enums;

#endregion

namespace HearthstoneReplays.Parser
{
	public class ParserState
	{
		public ParserState()
        {
            Logger.Log("Calling reset from ParserState constructor", "");
            //Reset();
		}

		public GameState GameState = new GameState();
        public NodeParser NodeParser = new NodeParser();
		public HearthstoneReplay Replay { get; set; }
		public Game CurrentGame { get; set; }
		public GameData GameData { get; set; }
		public SendChoices SendChoices { get; set; }
		public Choices Choices { get; set; }
		public Options Options { get; set; }
		public Option CurrentOption { get; set; }
		public object LastOption { get; set; }
		public int FirstPlayerId { get; set; }
	    public int CurrentPlayerId { get; set; }
		public ChosenEntities CurrentChosenEntites { get; set; }
        public bool Ended { get; set; }
        public int NumberOfCreates { get; set; }
        public bool ReconnectionOngoing { get; set; }
        //public string FullLog { get; set; } = "";

		private Node _node;
		public Node Node
		{
			get { return _node; }
			set
            {
                if (value != _node)
                {
                    if (_node != null 
                        // This works because Tag and TagChanges don't create new nodes
                        && (_node.Type == typeof(FullEntity) 
                                || _node.Type == typeof(ShowEntity) 
                                || _node.Type == typeof(ChangeEntity)))
                    {
                        EndAction();
                        if (_node.Type == typeof(ShowEntity))
                        {
                            GameState.ShowEntity(_node.Object as ShowEntity);
                        }
                        else if (_node.Type == typeof(FullEntity))
                        {
                            // In this case we just update the current state to whatever the game 
                            // tells us to
                            if (ReconnectionOngoing)
                            {
                                GameState.UpdateTagsForFullEntity(_node.Object as FullEntity);
                            }
                            else
                            {
                                GameState.FullEntity(_node.Object as FullEntity, false);
                            }
                        }
                        else if (_node.Type == typeof(ChangeEntity))
                        {
                            GameState.ChangeEntity(_node.Object as ChangeEntity);
                        }
                    }
                    if (_node != null && _node.Type == typeof(PlayerEntity))
                    {
                        GameState.PlayerEntity(_node.Object as PlayerEntity);
                    }
                    if (_node != null && _node.Type == typeof(GameEntity))
                    {
                        GameState.GameEntity(_node.Object as GameEntity);
                    }
                    if (_node != null && _node.Type == typeof(MetaData))
                    {
                        EndAction();
                    }
                    //HandleNodeUpdateEvent(_node, value);
                    this._node = value;
                }
            }
		}

		private Player _localPlayer;
		public Player LocalPlayer
        {
            get { return _localPlayer; }
        }

        public void SetLocalPlayer(Player value, DateTime timestamp, string data) { 
			_localPlayer = value;
            value.IsMainPlayer = true;
            var playerEntity = getPlayers().Find(player => player.PlayerId == value.PlayerId);
            playerEntity.IsMainPlayer = value.IsMainPlayer;
            NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    () => new GameEvent
                    {
                        Type = "LOCAL_PLAYER",
                        Value = this._localPlayer
                    },
                    false,
                    data) });
		}

		private Player _opponentPlayer;
		public Player OpponentPlayer
        {
            get { return _opponentPlayer; }
        }
        
        public void SetOpponentPlayer(Player value, DateTime timestamp, string data)
        {
            _opponentPlayer = value;
            value.IsMainPlayer = false;
            var playerEntity = getPlayers().Find(player => player.PlayerId == value.PlayerId);
            playerEntity.IsMainPlayer = value.IsMainPlayer;
            NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    () => new GameEvent
                    {
                        Type = "OPPONENT_PLAYER",
                        Value = this._opponentPlayer
                    },
                    false,
                    data) });
        }

		public void Reset()
		{
			GameState.Reset(this);
            NodeParser.Reset(this);
			Replay = new HearthstoneReplay();
			Replay.Games = new List<Game>();
			CurrentGame = new Game();
			this._localPlayer = null;
			this._opponentPlayer = null;
			Node = null;
			GameData = null;
			SendChoices = null;
			Choices = null;
			Options = null;
			CurrentOption = null;
			LastOption = null;
			FirstPlayerId = -1;
			CurrentPlayerId = -1;
			CurrentChosenEntites = null;
			Ended = false;
            ReconnectionOngoing = false;
            //FullLog = "";
            NumberOfCreates = 0;
            Logger.Log("resetting game state", "");
        }

        public void CreateNewNode(Node newNode)
        {
            NodeParser.NewNode(newNode);
        }

        public void EndAction()
        {
            NodeParser.CloseNode(Node);
        }

        public void EndCurrentGame()
		{
			Ended = true;
		}

		public void UpdateCurrentNode(params System.Type[] types)
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

		public void TryAssignLocalPlayer(DateTime timestamp, string data)
		{
			// Only assign the local player once
			if (LocalPlayer != null && OpponentPlayer != null)
			{
				return;
			}

			// Names are not assigned right away, so wait until all the data is present to notify
			foreach (PlayerEntity player in getPlayers())
			{
                // SOme games against AI, eg Bob
				if (player.Name == null && player.AccountHi != "0")
				{
					//Console.WriteLine("Player with no name: " + player);
					return;
				}
				var playerEntityIdTag = player.Tags.Where(t => t.Name == (int)GameTag.HERO_ENTITY).First();
				if (playerEntityIdTag == null)
				{
					return;
				}
			}

			//Console.WriteLine("Trying to assign local player");
			List<IEntityData> showEntities = CurrentGame.FilterGameData(typeof(ShowEntity)).Select(d => (IEntityData)d).ToList();
            // Happens when facing Bob
            if (showEntities.Count == 0)
            {
                showEntities = CurrentGame.FilterGameData(typeof(FullEntity)).Select(d => (IEntityData)d).ToList();
            }
            // This doesn't work because some cards are revealed when drawn (like Aranasi Bloodmother) and mess up with
            // this logic
            // Adding a zone = HAND check to circumbent this
			foreach (IEntityData entity in showEntities)
			{
				//Console.WriteLine("Considering entity: " + entity);
				if (entity.CardId != null && entity.CardId.Length > 0 
                    && GetTag(entity.Tags, GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT 
                    && GetTag(entity.Tags, GameTag.ZONE) == (int)Zone.HAND)
				{
					int entityId = entity.Entity;
					BaseEntity fullEntity = GetEntity(entityId);
					int controllerId = GetTag(fullEntity.Tags, GameTag.CONTROLLER);
					//Console.WriteLine("Passed first step: " + entityId + ", " + fullEntity + ", " + controllerId);
					foreach (PlayerEntity player in getPlayers())
					{
						if (GetTag(player.Tags, GameTag.CONTROLLER) == controllerId)
						{
							var newPlayer = Player.from(player);
							var playerEntityId = player.Tags.Where(t => t.Name == (int)GameTag.HERO_ENTITY).First().Value;
							FullEntity playerEntity = CurrentGame.Data
								.Where(d => d is FullEntity)
								.Select(d => (FullEntity)d)
								.Where(e => e.Id == playerEntityId)
								.First();
							newPlayer.CardID = playerEntity.CardId;
                            SetLocalPlayer(newPlayer, timestamp, data);
						}
					}
					if (LocalPlayer != null)
					{
						foreach (PlayerEntity player in getPlayers())
						{
							if (player.Id == LocalPlayer.Id)
							{
								continue;
							}
							var newPlayer = Player.from(player);
							var playerEntityId = player.Tags.Where(t => t.Name == (int)GameTag.HERO_ENTITY).First().Value;
							FullEntity playerEntity = CurrentGame.Data
								.Where(d => d is FullEntity)
								.Select(d => (FullEntity)d)
								.Where(e => e.Id == playerEntityId)
								.First();
							newPlayer.CardID = playerEntity.CardId;
                            SetOpponentPlayer(newPlayer, timestamp, data);
						}
						return;
					}
				}
			}
		}

		public int GetTag(List<Tag> tags, GameTag tag)
		{
			Tag ret = tags.FirstOrDefault(t => t.Name == (int)tag);
			return ret == null ? -1 : ret.Value;
		}

		public BaseEntity GetEntity(int id)
		{
			return CurrentGame.FilterGameData(typeof(FullEntity), typeof(PlayerEntity))
				.Select(data => (BaseEntity)data).ToList()
				.Where(e => e.Id == id)
				.First();
		}

		//private void HandleNodeUpdateEvent(Node oldNode, Node newNode)
		//{
		//	if (oldNode != null && oldNode.Type == typeof(FullEntity))
		//	{
		//		//Logger.Log("Handling node update", oldNode.Type);
		//		GameState.FullEntityNodeComplete((oldNode.Object as FullEntity));
		//	}
		//}
	}
}