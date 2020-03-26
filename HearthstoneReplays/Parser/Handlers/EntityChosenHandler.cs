using System;
using System.Collections.Generic;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Parser.ReplayData;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

namespace HearthstoneReplays.Parser.Handlers
{
	public class EntityChosenHandler
	{
		private Helper helper = new Helper();

		public void Handle(DateTime timestamp, string data, ParserState state)
        {
            // This happens for the first options when spectating a game that has already started
            if (state.CurrentGame == null || state.Node == null)
            {
                return;
            }

            data = data.Trim();
			var match = Regexes.EntitiesChosenRegex.Match(data);
			if(match.Success)
			{
				/*NOTE: in 10357, "Player" is bugged, it's treating a player ID
				as an entity ID, resulting in "Player=GameEntity"
				For our own sanity we keep the old playerID logic from the
				previous builds, we'll change to "player" when it's fixed.*/
				var rawEntity = match.Groups[1].Value;
				var rawPlayer = match.Groups[2].Value;
				var count = int.Parse(match.Groups[3].Value);
				var entity = helper.ParseEntity(rawEntity, state);
				var player = helper.ParseEntity(rawPlayer, state);
				var cEntities = new ChosenEntities {Entity = entity, PlayerId = player, Count = count, Choices = new List<Choice>(), TimeStamp = timestamp};

				if (state.Node.Type == typeof(Game))
					((Game)state.Node.Object).AddData(cEntities);
				else if (state.Node.Type == typeof(Action))
					((Action)state.Node.Object).Data.Add(cEntities);
				else
					throw new Exception("Invalid node " + state.Node.Type + " -- " + data);
				state.CurrentChosenEntites = cEntities;
				return;
			}
			match = Regexes.EntitiesChosenEntitiesRegex.Match(data);
			if(match.Success)
			{
				var index = int.Parse(match.Groups[1].Value);
				var rawEntity = match.Groups[2].Value;
				var entity = helper.ParseEntity(rawEntity, state);
				var choice = new Choice {Entity = entity, Index = index, TimeStamp = timestamp};
				state.CurrentChosenEntites.Choices.Add(choice);

				state.CreateNewNode(new Node(typeof(Choice), choice, 0, null, data)); // It's not really a new node, but just a hack
				return;
			}
			Console.WriteLine("Warning: Unhandled chosen entities: " + data);
		}
	}
}