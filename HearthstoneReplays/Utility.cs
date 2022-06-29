#region

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

#endregion

namespace HearthstoneReplays
{
	internal static class Utility
	{
		internal static object DeepClone(object obj)
		{
			object objResult = null;
			using(MemoryStream ms = new MemoryStream())
			{
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(ms, obj);

				ms.Position = 0;
				objResult = bf.Deserialize(ms);
			}
			return objResult;
		}

		internal static long GetUtcTimestamp(DateTime time)
        {
			var currentTimeZone = TimeZone.CurrentTimeZone;
			var offset = currentTimeZone.GetUtcOffset(time);
			return (long)time.Subtract(offset).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
		}
	}
}