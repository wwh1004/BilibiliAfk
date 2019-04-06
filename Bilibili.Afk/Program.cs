using System;
using Bilibili.Api;
using Bilibili.Api.Settings;

namespace Bilibili.Afk {
	internal static class Program {
		private static void Main(string[] args) {
			UserList users;

			try {
				GlobalSettings.LoadAll();
			}
			catch {
				Logger.LogError("缺失或无效配置文件，请检查是否添加\"Users.json\"");
				Console.ReadKey(true);
				return;
			}
			users = GlobalSettings.Users;
			User user = users[0];
			var result = user.Login().GetAwaiter().GetResult();
			Console.ReadKey(true);
		}
	}
}
