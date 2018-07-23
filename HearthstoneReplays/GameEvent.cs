using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HearthstoneReplays
{
	public class GameEvent
	{
		public string Type { get; set; }
		public Object Value { get; set; }

		public override string ToString() {
			return "GameEvent: " + Type + " (" + Value + ")";
		}
	}
}
