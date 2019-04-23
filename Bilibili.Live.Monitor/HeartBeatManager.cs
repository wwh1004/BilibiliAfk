using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Bilibili.Api;
using Bilibili.Settings;

namespace Bilibili.Live.Monitor {
	internal sealed class HeartBeatManager {
		private readonly static HeartBeatManager _instance = new HeartBeatManager();

		private readonly List<TcpClient> _clients;
		private readonly object _syncRoot = new object();

		public static HeartBeatManager Instance => _instance;

		private HeartBeatManager() {
			_clients = new List<TcpClient>();
			new Thread(() => SafeCaller.Loop(ExecuteLoop)) {
				IsBackground = true,
				Name = "HeartBeatManager"
			}.Start();
		}

		public void Add(TcpClient client) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			lock (_syncRoot)
				_clients.Add(client);
		}

		public void Remove(TcpClient client) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			lock (_syncRoot)
				_clients.Remove(client);
		}

		private void ExecuteLoop() {
			while (true) {
				DateTime startTime;
				TimeSpan span;

				startTime = DateTime.Now;
				lock (_syncRoot) {
					// 防止此时进行Add和Remove操作
					if (_clients.Count > 50) {
						Parallel.ForEach(_clients, client => {
							if (client.Connected && client.GetStream().CanWrite)
								SafeCaller.Call(() => DanmuApi.SendHeartBeat(client), true);
							Thread.Sleep(40);
						});
					}
					else {
						foreach (TcpClient client in _clients) {
							if (client.Connected && client.GetStream().CanWrite)
								SafeCaller.Call(() => DanmuApi.SendHeartBeat(client), true);
							Thread.Sleep(40);
						}
					}
				}
				span = DateTime.Now - startTime;
				// 获取发送心跳耗时
				span = DanmuApi.HeartBeatInterval - span;
				// 计算还需要等待的时间
				if (span.Ticks > 0)
					Thread.Sleep(span);
				else
					GlobalSettings.Logger.LogWarning($"心跳发送延时过度: {span.Milliseconds}ms");
			}
		}
	}
}
