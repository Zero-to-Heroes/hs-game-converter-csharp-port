#region

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

#endregion

namespace HearthstoneReplayTests
{
	internal class TestDataReader
	{
		public static List<string> GetInputFile(string filename)
		{
			List<string> lines = new List<string>();
			Assembly thisAssembly = Assembly.GetExecutingAssembly();
			using (StreamReader reader = new StreamReader(thisAssembly.GetManifestResourceStream("HearthstoneReplayTests.TestData." + filename), Encoding.UTF8)) {
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					lines.Add(line);
				}
			}
			return lines;
		}
	}
}