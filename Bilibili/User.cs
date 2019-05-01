using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using Bilibili.Settings;
using Newtonsoft.Json;

namespace Bilibili {
	/// <summary>
	/// 表示一个Bilibili用户
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	public sealed class User : IDisposable {
		[JsonProperty("Account")]
		private readonly string _account;
		[JsonProperty("Password")]
		private readonly string _password;
		private readonly HttpClient _client;
		private readonly Dictionary<string, string> _pcHeaders;
		private readonly Dictionary<string, string> _appHeaders;
		[JsonProperty("LoginData", NullValueHandling = NullValueHandling.Ignore)]
		private readonly Dictionary<string, string> _loginData;
		private bool _isDisposed;

		/// <summary>
		/// 账号（手机号/邮箱）
		/// </summary>
		public string Account => _account;

		/// <summary>
		/// 密码
		/// </summary>
		public string Password => _password;

		/// <summary />
		public HttpClient Client => _client;

		/// <summary />
		public Dictionary<string, string> PCHeaders => _pcHeaders;

		/// <summary />
		public Dictionary<string, string> AppHeaders => _appHeaders;

		/// <summary />
		public Dictionary<string, string> LoginData => _loginData;

		/// <summary>
		/// 是否存在登录数据（不保证数据有效，只判断是否存在数据）
		/// </summary>
		public bool HasLoginData => _loginData.Count != 0;

		/// <summary>
		/// 构造器（用于反序列化，虽然Json.NET可以使用有参构造器，但是如果程序被混淆，反序列化将失败）
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public User() {
			_client = new HttpClient(new HttpClientHandler { UseCookies = false }) {
				Timeout = TimeSpan.FromMilliseconds(3000)
			};
			_pcHeaders = new Dictionary<string, string>();
			_appHeaders = new Dictionary<string, string>();
			_loginData = new Dictionary<string, string>();
			Initialize();
		}

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="account">账号</param>
		/// <param name="password">密码</param>
		public User(string account, string password) : this() {
			if (account == null)
				throw new ArgumentNullException(nameof(account));
			if (password == null)
				throw new ArgumentNullException(nameof(password));

			_account = account;
			_password = password;
		}

		/// <summary>
		/// 初始化
		/// </summary>
		public void Initialize() {
			_pcHeaders.Clear();
			UpdateDictionary(_pcHeaders, GlobalSettings.Bilibili.PCHeaders);
			_appHeaders.Clear();
			UpdateDictionary(_appHeaders, GlobalSettings.Bilibili.AppHeaders);
		}

		/// <summary>
		/// 将登录数据导入到 <see cref="PCHeaders"/> 和 <see cref="AppHeaders"/>
		/// </summary>
		public void ImportLoginData() {
			if (!HasLoginData)
				return;
			UpdateDictionary(_pcHeaders, _loginData);
			UpdateDictionary(_appHeaders, _loginData);
		}

		/// <summary>
		/// 清除所有缓存数据以及登录数据
		/// </summary>
		public void Clear() {
			Initialize();
			_loginData.Clear();
		}

		/// <summary />
		public override string ToString() {
			return _account;
		}

		/// <summary />
		public void Dispose() {
			if (_isDisposed)
				return;
			_client.Dispose();
			_isDisposed = true;
		}

		private static void UpdateDictionary(Dictionary<string, string> target, Dictionary<string, string> source) {
			foreach (KeyValuePair<string, string> item in source)
				target[item.Key] = item.Value;
		}
	}
}
