using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace Bilibili {
	/// <summary>
	/// 用户列表
	/// </summary>
	public sealed class Users : List<User> {
		/// <summary>
		/// 加载配置
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static Users FromJson(string json) {
			Users users;

			users = JsonConvert.DeserializeObject<Users>(json);
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

		/// <summary>
		/// 获取用户列表的默认文件名，由调用方所属程序集决定
		/// </summary>
		/// <returns></returns>
		public static string GetDefaultFileName() {
			return @"Settings\" + ((AssemblyProductAttribute)Assembly.GetCallingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product + ".Users.json";
			// 因为使用了GetCallingAssembly，所以这个方法不能被混淆，这个方法的返回值与调用堆栈，调用方有关
		}
	}
}
