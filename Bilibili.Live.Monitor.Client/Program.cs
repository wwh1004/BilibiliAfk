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
			_ = StartAsync(0, 1000);
			while (true)
				Thread.Sleep(int.MaxValue);
		}

		private static async Task StartAsync(uint start, uint end) {
			uint[] roomIds;
			int count;
			TimeSpan interval;

			roomIds = LiveApi.GetRoomIdsDynamicAsync(start, end).Result;
			count = (int)(end - start);
			interval = TimeSpan.FromMilliseconds(DanmuApi.HeartBeatInterval.TotalMilliseconds / count);
			for (int i = 0; i < count; i++) {
				DateTime startTime;
				WrappedDanmuMonitor wrappedDanmuMonitor;
				TimeSpan span;

				startTime = DateTime.Now;
				wrappedDanmuMonitor = new WrappedDanmuMonitor(() => {
					DanmuMonitor danmuMonitor;

					danmuMonitor = new DanmuMonitor(roomIds[i], (int)start + i, i == 0);
					danmuMonitor.DanmuHandler += DanmuMonitor_DanmuHandler;
					return danmuMonitor;
				});
				wrappedDanmuMonitor.Execute();
				span = interval - (DateTime.Now - startTime);
				if (span.Ticks > 0)
					await Task.Delay(span);
				// "await Task.Delay(span)"的精确度最低，据说是以15ms为单位
				// "Thread.Sleep(span)"精度稍高，但还是不如"new ManualResetEvent(false).WaitOne(span)"
				// 但是不清楚为什么，使用"new ManualResetEvent(false).WaitOne(span)"的效果和"await Task.Delay(span)"差不多
				// 所以还是使用资源占用最小的"await Task.Delay(span)"
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
