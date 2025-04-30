#region

using System;
using System.Collections.Generic;
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

	// This is way too slow
    //public class IncludeJsonIgnoreContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    //{
    //    protected override IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(Type type, Newtonsoft.Json.MemberSerialization memberSerialization)
    //    {
    //        var properties = base.CreateProperties(type, memberSerialization);

    //        // Ensure all properties are serialized, even those with [JsonIgnore]
    //        foreach (var property in properties)
    //        {
    //            property.Ignored = false; // Override the Ignored flag
    //        }

    //        return properties;
    //    }
    //}
}