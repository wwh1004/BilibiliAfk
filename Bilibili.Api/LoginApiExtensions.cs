using System;
using System.Linq;
using System.Threading.Tasks;
using Bilibili.Api.Settings;
using Newtonsoft.Json.Linq;

namespace Bilibili.Api {
	/// <summary>
	/// 登录API扩展类，提供快速操作
	/// </summary>
	public static class LoginApiExtensions {
		/// <summary>
		/// 登录
		/// </summary>
		/// <param name="user">用户</param>
		/// <returns></returns>
		public static async Task<bool> Login(this User user) {
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			bool flag;
			int expiresIn;
			string key;
			string json;
			JObject result;

			if (user.HasLoginData) {
				// 如果以前登录过，判断一下需不需要重新登录
				// 这个API每次登录有效时间是720小时（expires_in=2592000）
				(flag, expiresIn) = await user.GetExpiresIn();
				if (flag) {
					// Token有效
					if (expiresIn < 1800) {
						// Token有效，但是有效时间太短，小于半个小时
						Logger.LogInfo("Token有效时间不足");
						return await user.RefreshToken();
					}
					else {
						// Token有效时间足够
						Logger.LogInfo($"用户\"{user}\"使用缓存Token登录成功");
						Logger.LogInfo($"Token有效时间还剩：{Math.Round(expiresIn / 3600d, 1)} 小时");
						return true;
					}
				}
			}
			// 不存在登录数据，这是第一次登录
			try {
				key = await LoginApi.GetKey(user);
				json = await LoginApi.Login(user, key, null);
				result = JObject.Parse(json);
			}
			catch (Exception ex) {
				Logger.LogError($"用户\"{user}\"登录失败");
				throw new ApiException(ex);
			}
			if ((int)result["code"] == 0 && (int)result["data"]["status"] == 0) {
				// 登录成功，保存数据直接返回
				Logger.LogInfo($"用户\"{user}\"登录成功");
				UpdateLoginData(user, result);
				GlobalSettings.SaveUsers();
				return true;
			}
			else if ((int)result["code"] == -105)
				// 需要验证码
				return await LoginWithCaptcha(user, key);
			else {
				// 其它错误
				Logger.LogError($"用户\"{user}\"登录失败");
				Logger.LogError($"错误信息：{Utils.FormatJson(json)}");
				return false;
			}
		}

		/// <summary>
		/// 尝试获取Token过期时间
		/// </summary>
		/// <param name="user">用户</param>
		/// <returns></returns>
		public static async Task<(bool, int)> GetExpiresIn(this User user) {
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			JObject result;

			try {
				string json = await LoginApi.GetInfo(user);
				result = JObject.Parse(json);
			}
			catch (Exception ex) {
				throw new ApiException(ex);
			}
			if ((int)result["code"] == 0 && result["data"]["mid"] != null)
				return (true, (int)result["data"]["expires_in"]);
			else
				return (false, 0);
		}

		/// <summary>
		/// 刷新Token
		/// </summary>
		/// <param name="user">用户</param>
		/// <returns></returns>
		public static async Task<bool> RefreshToken(this User user) {
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			string json;
			JObject result;

			try {
				json = await LoginApi.RefreshToken(user);
				result = JObject.Parse(json);
			}
			catch (Exception ex) {
				throw new ApiException(ex);
			}
			if ((int)result["code"] == 0 && result["data"]["token_info"]["mid"] != null) {
				Logger.LogInfo($"Token\"{user}\"刷新成功");
				UpdateLoginData(user, result);
				GlobalSettings.SaveUsers();
				return true;
			}
			else {
				Logger.LogError($"Token\"{user}\"刷新失败");
				Logger.LogError($"错误信息：{Utils.FormatJson(json)}");
				return false;
			}
		}

		private static async Task<bool> LoginWithCaptcha(User user, string key) {
			string json;
			JObject result;

			try {
				string captcha;

				captcha = await LoginApi.SolveCaptcha(await LoginApi.GetCaptcha(user));
				json = await LoginApi.Login(user, key, captcha);
				result = JObject.Parse(json);
			}
			catch (Exception ex) {
				Logger.LogError($"用户\"{user}\"登录失败");
				throw new ApiException(ex);
			}
			if ((int)result["code"] == 0 && (int)result["data"]["status"] == 0) {
				// 登录成功，保存数据直接返回
				Logger.LogInfo($"用户\"{user}\"登录成功");
				UpdateLoginData(user, result);
				GlobalSettings.SaveUsers();
				return true;
			}
			else {
				// 其它错误
				Logger.LogError($"用户\"{user}\"登录失败");
				Logger.LogError($"错误信息：{Utils.FormatJson(json)}");
				return false;
			}
		}

		private static void UpdateLoginData(User user, JToken data) {
			JToken tokenInfo;
			JToken cookies;

			data = data["data"];
			tokenInfo = data["token_info"];
			cookies = data["cookie_info"]["cookies"];
			user.LoginData["access_key"] = (string)tokenInfo["access_token"];
			user.LoginData["cookie"] = string.Join("&", cookies.Select(t => (string)t["name"] + "=" + (string)t["value"]));
			user.LoginData["csrf"] = (string)cookies[0]["value"];
			user.LoginData["refresh_token"] = (string)tokenInfo["refresh_token"];
			user.LoginData["uid"] = (string)cookies[1]["value"];
		}
	}
}