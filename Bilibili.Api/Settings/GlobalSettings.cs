using System.IO;

namespace Bilibili.Api.Settings {
	/// <summary>
	/// 全局设置
	/// </summary>
	public static class GlobalSettings {
		private const string BILIBILISETTINGS_PATH = @"Settings\BilibiliSettings.json";
		private const string USERS_PATH = @"Settings\Users.json";

		private static BilibiliSettings _bilibili;
		private static UserList _users;
		private static readonly object _syncRoot = new object();

		/// <summary />
		public static BilibiliSettings Bilibili => _bilibili;

		/// <summary />
		public static UserList Users => _users;

		/// <summary>
		/// 加载设置文件
		/// </summary>
		public static void LoadAll() {
			lock (_syncRoot) {
				_bilibili = BilibiliSettings.FromJson(File.ReadAllText(BILIBILISETTINGS_PATH));
				_users = UserList.FromJson(File.ReadAllText(USERS_PATH));
			}
		}

		/// <summary>
		/// 保存用户列表
		/// </summary>
		public static void SaveUsers() {
			lock (_syncRoot)
				File.WriteAllText(USERS_PATH, _users.ToJson());
		}
	}
}
