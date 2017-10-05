#region

using System;
using System.Collections.Generic;
using HearthstoneReplays;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace HearthstoneReplayTests
{
	[TestClass]
	public class ReplayDataTests
	{
		[TestMethod]
		public void Test()
		{ 
			List<string> logFile = TestDataReader.GetInputFile("Power_1.log.txt");
			HearthstoneReplay replay = new ReplayParser().FromString(logFile);
			string xml = new ReplayConverter().xmlFromReplay(replay);
			Console.Write(xml);
		}
	}
}
