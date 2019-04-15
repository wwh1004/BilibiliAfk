using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bilibili.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bilibili.Api {
	/// <summary>
	/// 登录API
	/// </summary>
	public static class LoginApi {
		private const string CAPTCHA_URL = "https://passport.bilibili.com/captcha";
		private const string LOGIN_EXIT_URL = "https://passport.bilibili.com/login?act=exit";
		private const string OAUTH2_GETKEY_URL = "https://passport.bilibili.com/api/oauth2/getKey";
		private const string OAUTH2_INFO_URL = "https://passport.bilibili.com/api/v2/oauth2/info";
		private const string OAUTH2_LOGIN_URL = "https://passport.bilibili.com/api/v2/oauth2/login";
		private const string OAUTH2_REFRESH_TOKEN_URL = "https://passport.bilibili.com/api/v2/oauth2/refresh_token";
		private const string SOLVE_CAPTCHA_URL = "http://115.159.205.242:19951/captcha/v1";

		private static Dictionary<string, string> General => GlobalSettings.Bilibili.General;

		/// <summary>
		/// 获取验证码
		/// </summary>
		/// <param name="user">用户</param>
		/// <returns></returns>
		public static async Task<byte[]> GetCaptcha(User user) {
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (HttpResponseMessage response = await user.Client.SendAsync(HttpMethod.Get, CAPTCHA_URL))
				return await response.Content.ReadAsByteArrayAsync();
		}

		/// <summary>
		/// 登出
		/// </summary>
		/// <param name="user">用户</param>
		/// <returns></returns>
		public static async Task<bool> Logout(User user) {
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (HttpResponseMessage response = await user.Client.SendAsync(HttpMethod.Get, LOGIN_EXIT_URL, null, user.PCHeaders))
				return true;
		}

		/// <summary>
		/// 获取Key
		/// </summary>
		/// <param name="user">用户</param>
		/// <returns></returns>
		public static async Task<string> GetKey(User user) {
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			FormUrlEncodedCollection parameters;

			parameters = new FormUrlEncodedCollection {
				{ "appkey", General["appkey"] }
			};
			parameters.SortAndSign();
			using (HttpResponseMessage response = await user.Client.SendAsync(HttpMethod.Post, OAUTH2_GETKEY_URL, parameters, null))
				return await response.Content.ReadAsStringAsync();
		}

		/// <summary>
		/// 获取信息
		/// </summary>
		/// <param name="user">用户</param>
		/// <returns></returns>
		public static async Task<string> GetInfo(User user) {
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			FormUrlEncodedCollection parameters;

			parameters = new FormUrlEncodedCollection {
				{ "access_key", user.LoginData["access_key"] },
				{ "ts", ApiUtils.GetTimeStamp().ToString() }
			};
			parameters.AddRange(user.LoginData["cookie"].Split(';').Select(item => {
				string[] pair;

				pair = item.Split('=');
				return new KeyValuePair<string, string>(pair[0], pair[1]);
			}));
			parameters.AddRange(General);
			parameters.SortAndSign();
			using (HttpResponseMessage response = await user.Client.SendAsync(HttpMethod.Get, OAUTH2_INFO_URL, parameters, user.AppHeaders))
				return await response.Content.ReadAsStringAsync();
		}

		/// <summary>
		/// 登录
		/// </summary>
		/// <param name="user">用户</param>
		/// <param name="jsonKey">Key</param>
		/// <param name="captcha">验证码</param>
		/// <returns></returns>
		public static async Task<string> Login(User user, string jsonKey, string captcha) {
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrEmpty(jsonKey))
				throw new ArgumentNullException(nameof(jsonKey));

			JToken loginKey;
			string rsaKey;
			RSAParameters rsaParameters;
			FormUrlEncodedCollection parameters;

			loginKey = JObject.Parse(jsonKey)["data"];
			rsaKey = loginKey["key"].ToString();
			rsaKey = rsaKey.Replace("\n", string.Empty).Substring(26, rsaKey.Length - 56);
			rsaParameters = ApiUtils.ParsePublicKey(rsaKey);
			parameters = new FormUrlEncodedCollection {
				{ "username", user.Account },
				{ "password", ApiUtils.RsaEncrypt(loginKey["hash"] + user.Password, rsaParameters) },
				{ "captcha", captcha ?? string.Empty }
			};
			parameters.AddRange(General);
			parameters.SortAndSign();
			using (HttpResponseMessage response = await user.Client.SendAsync(HttpMethod.Post, OAUTH2_LOGIN_URL, parameters, null))
				return await response.Content.ReadAsStringAsync();
		}

		/// <summary>
		/// 刷新Token
		/// </summary>
		/// <param name="user">用户</param>
		/// <returns></returns>
		public static async Task<string> RefreshToken(User user) {
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			FormUrlEncodedCollection parameters;

			parameters = new FormUrlEncodedCollection {
				{ "access_key", user.LoginData["access_key"] },
				{ "refresh_token", user.LoginData["refresh_token"] },
				{ "ts", ApiUtils.GetTimeStamp().ToString() }
			};
			parameters.AddRange(user.LoginData["cookie"].Split(';').Select(item => {
				string[] pair;

				pair = item.Split('=');
				return new KeyValuePair<string, string>(pair[0], pair[1]);
			}));
			parameters.AddRange(General);
			parameters.SortAndSign();
			using (HttpResponseMessage response = await user.Client.SendAsync(HttpMethod.Post, OAUTH2_REFRESH_TOKEN_URL, parameters, user.AppHeaders))
				return await response.Content.ReadAsStringAsync();
		}

		/// <summary>
		/// 识别验证码
		/// </summary>
		/// <param name="captcha"></param>
		/// <returns></returns>
		public static async Task<string> SolveCaptcha(byte[] captcha) {
			string json;

			json = JsonConvert.SerializeObject(new {
				image = Convert.ToBase64String(captcha)
			});
			using (HttpClient client = new HttpClient())
			using (HttpResponseMessage response = await client.SendAsync(HttpMethod.Post, SOLVE_CAPTCHA_URL, null, null, json, "application/json"))
				return JObject.Parse(await response.Content.ReadAsStringAsync())["message"].ToString();
		}
	}
}
