using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Bilibili.Api;
using Bilibili.Settings;
using Newtonsoft.Json;

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
			uint[] roomIds = LiveApi.GetRoomIdsDynamicAsync(0, 5).Result;
			TaskFactory taskFactory = LimitedConcurrencyLevelUtils.TaskFactory;
			taskFactory = Task.Factory;
			for (int i = 0; i < roomIds.Length; i++) {
				uint roomId = roomIds[i];
				DanmuMonitor danmuMonitor = new DanmuMonitor(roomId /*4816623*/) {
					Id = i
				};
				danmuMonitor.DanmuHandler += (sender, e) => {
					//GlobalSettings.Logger.LogInfo(e.Danmu.Data.Length.ToString());
				};
				_ = taskFactory.StartNew(() => danmuMonitor.ExecuteAsync()).Unwrap();
			}
			//Thread.Sleep(5000);
			//danmuMonitor.Dispose();
			Console.WriteLine("ffffffffffffffffffffffffffffffff");
			Console.ReadKey(true);
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
