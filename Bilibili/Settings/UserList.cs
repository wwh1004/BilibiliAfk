using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bilibili.Settings {
	/// <summary>
	/// 用户列表
	/// </summary>
	public sealed class UserList : List<User> {
		/// <summary>
		/// 加载配置
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static UserList FromJson(string json) {
			UserList users;

			users = JsonConvert.DeserializeObject<UserList>(json);
			foreach (User user in users)
				user.ImportLoginData();
			return users;
		}

		/// <summary>
		/// 转换为JSON
		/// </summary>
		/// <returns></returns>
		public string ToJson() {
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}
}
