using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Bilibili.Api;
using Bilibili.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bilibili.Live.Monitor {
	internal static class Program {
		private static async Task Main() {
			Console.Title = GetTitle();
			GlobalSettings.Logger = Logger.Instance;
			if (!BitConverter.IsLittleEndian) {
				GlobalSettings.Logger.LogWarning("在BigEndian模式的CPU下工作可能导致程序出错");
				GlobalSettings.Logger.LogWarning("如果出现错误，请创建issue");
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
			await StartAsync(0, 300);
		}

		private static async Task StartAsync(uint start, uint end) {
			uint[] roomIds;
			int count;
			TimeSpan interval;

			roomIds = await LiveApi.GetRoomIdsDynamicAsync(start, end);
			count = (int)(end - start);
			interval = TimeSpan.FromMilliseconds(DanmuApi.HeartBeatInterval.TotalMilliseconds / count);
			for (uint i = start; i < end; i++) {
				DateTime startTime;
				DanmuMonitor danmuMonitor;
				TimeSpan span;

				startTime = DateTime.Now;
				danmuMonitor = new DanmuMonitor(roomIds[i], (int)i, i == start);
				danmuMonitor.DanmuHandler += DanmuMonitor_DanmuHandler;
				danmuMonitor.Execute();
				span = interval - (DateTime.Now - startTime);
				if (span.Ticks > 0)
					await Task.Delay(span);
			}
		}

		private static void DanmuMonitor_DanmuHandler(object sender, DanmuHandlerEventArgs e) {
			JObject json;

			json = e.Danmu.Json;
			switch ((string)json["cmd"]) {
			case "GUARD_MSG":
			case "SPECIAL_GIFT":
				GlobalSettings.Logger.LogInfo(FormatJson(json.ToString()));
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
