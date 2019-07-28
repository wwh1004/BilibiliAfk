using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Bilibili.Api;
using Bilibili.Settings;

namespace Bilibili.Afk {
	internal static class Program {
		private static async Task Main() {
			string usersFilePath;
			Users users;

			Console.Title = GetTitle();
			GlobalSettings.Logger = Logger.Instance;
			if (!BitConverter.IsLittleEndian) {
				GlobalSettings.Logger.LogWarning("在BigEndian模式的CPU下工作可能导致程序出错");
				GlobalSettings.Logger.LogWarning("如果出现错误，请创建issue");
			}
			usersFilePath = Path.Combine(Environment.CurrentDirectory, "Settings", GetAssemblyAttribute<AssemblyProductAttribute>().Product + ".Users.json");
			try {
				GlobalSettings.LoadAll();
				users = Users.FromJson(File.ReadAllText(usersFilePath));
			}
			catch (Exception ex) {
				GlobalSettings.Logger.LogException(ex);
				GlobalSettings.Logger.LogError($"缺失或无效配置文件，请检查是否添加\"{usersFilePath}\"");
				Console.ReadKey(true);
				return;
			}
			LoginApiExtensions.LoginDataUpdated += (sender, e) => File.WriteAllText(usersFilePath, users.ToJson());
			User user = users[0];
			await user.Login();
			Console.ReadKey(true);
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
