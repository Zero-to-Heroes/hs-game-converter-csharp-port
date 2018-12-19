using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HearthstoneReplays
{
	public class GameEventHandler
	{
		public static Action<string> EventProvider;
		public static void Handle(GameEvent gameEvent) {
			EventProvider?.Invoke(JsonConvert.SerializeObject(gameEvent));
		}
	}
}
