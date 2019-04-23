using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bilibili.Api;
using Bilibili.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bilibili.Live.Monitor {
	internal static class Program {
		private static void Main(string[] args) {
			Console.Title = GetTitle();
			GlobalSettings.Logger = Logger.Instance;
			if (!BitConverter.IsLittleEndian) {
				GlobalSettings.Logger.LogWarning("在BigEndian模式的CPU下工作可能导致程序出错");
				GlobalSettings.Logger.LogWarning("如果出现错误，请创建issue");
				//throw new PlatformNotSupportedException();
			}
			try {
				GlobalSettings.LoadAll();
			}
			catch (Exception ex) {
				GlobalSettings.Logger.LogException(ex);
				GlobalSettings.Logger.LogError("缺失或无效配置文件");
				Console.ReadKey(true);
				return;
			}
			uint[] roomIds = LiveApi.GetRoomIdsDynamicAsync(0, 3000).Result;
			Parallel.For(0, roomIds.Length, i => {
				DanmuMonitor danmuMonitor = new DanmuMonitor(roomIds[i] /*4816623*/) {
					Id = i
				};
				danmuMonitor.DanmuHandler += DanmuMonitor_DanmuHandler;
				_ = danmuMonitor.ExecuteLoopAsync();
				_ = danmuMonitor.ExecuteHeartBeatLoopAsync();
				Thread.Sleep(100);
			});
			//for (int i = 0; i < roomIds.Length; i++) {
			//	uint roomId = roomIds[i];
			//	DanmuMonitor danmuMonitor = new DanmuMonitor(roomId /*4816623*/) {
			//		Id = i
			//	};
			//	danmuMonitor.DanmuHandler += DanmuMonitor_DanmuHandler;
			//	_ = danmuMonitor.ExecuteLoopAsync();
			//	_ = danmuMonitor.ExecuteHeartBeatLoopAsync();
			//	Thread.Sleep(100);
			//}
			while (true)
				Thread.Sleep(int.MaxValue);
			//Console.ReadKey(true);
		}

		private static void DanmuMonitor_DanmuHandler(object sender, DanmuHandlerEventArgs e) {
			JObject json;

			json = e.Danmu.Json;
			switch ((string)json["cmd"]) {
			case "GUARD_MSG":
			case "SPECIAL_GIFT":
				GlobalSettings.Logger.LogInfo(json.ToString());
				break;
			}
		}

		public static string FormatJson(string json) {
			using (StringWriter writer = new StringWriter())
			using (JsonTextWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
			using (StringReader reader = new StringReader(json))
			using (JsonTextReader jsonReader = new JsonTextReader(reader)) {
				jsonWriter.WriteToken(jsonReader);
				return writer.ToString();
			}
		}

		public static string GetTitle() {
			string productName;
			string version;

			productName = GetAssemblyAttribute<AssemblyProductAttribute>().Product;
			version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			return $"{productName} v{version}";
		}

		private static T GetAssemblyAttribute<T>() {
			return (T)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false)[0];
		}
	}
}
