#region

using System;
using System.Collections.Generic;

#endregion

namespace HearthstoneReplays.Parser
{
	public class SubSpell
	{
		public DateTime Timestamp { get; set; }

		public string Prefab { get; set; }

		public int Source { get; set; }

		public IList<int> Targets { get; set; }
	}
}