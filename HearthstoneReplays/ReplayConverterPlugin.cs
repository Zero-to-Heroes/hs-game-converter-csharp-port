using System;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Threading.Tasks;
using HearthstoneReplays;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.Entities;

namespace HearthstoneReplays
{
	public class ReplayConverterPlugin
	{
		// a global event that triggers with two parameters:
		//
		// plugin.get().onGlobalEvent.addListener(function(first, second) {
		//  ...
		// });
		public event Action<object, object> onGlobalEvent;


		// plugin.get().convertLogsToXml(xmlLogs, function(result) {
		//   console.log(result);
		// });
		// 
		// notice how we will always call the callback on a new thread
		public void convertLogsToXml(string logs, Action<object> callback)
		{
			if (callback == null)
			{
				onGlobalEvent("No callback, returning", logs);
				return;
			}

			Task.Run(() => {
				try
				{
					XmlSerializer Serializer = new XmlSerializer(typeof(FullEntity));
					//Serializer = new XmlSerializer(typeof(HearthstoneReplay));
					string replayXml = new ReplayConverter().xmlFromLogs(logs);
					onGlobalEvent("Serialized", replayXml.Length);
					callback(replayXml);
				}
				catch (Exception e)
				{
					onGlobalEvent("Exception when parsing game " + e.GetBaseException(), logs);
					callback(null);
				}
			});
		}

		//plugin.get().onGlobalEvent.addListener(function(first, second)
		//{
		//  ...
		// });
		
		// plugin.get().triggerGlobalEvent();
		public void triggerGlobalEvent(string first, string second)
		{
			if (onGlobalEvent == null)
			{
				return;
			}

			Task.Run(() =>
			{
				onGlobalEvent(first, second);
			});
		}
	}
}
