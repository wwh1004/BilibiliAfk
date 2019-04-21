using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Bilibili.Api;
using Bilibili.Settings;

namespace Bilibili.Live.Monitor {
	/// <summary>
	/// 弹幕监视器
	/// </summary>
	public sealed class DanmuMonitor : IDisposable {
		private readonly uint[] _roomIds;
		private readonly TcpClient[] _clients;
		private readonly bool[] _isConnecteds;
		private readonly Thread _connectionKeeper;
		private readonly Thread _heartBeatSender;
		private readonly Thread _danmuHandler;
		private readonly List<Socket> _cachedCheckRead;
		private bool _isDisposed;

		/// <summary>
		/// 房间ID
		/// </summary>
		public uint[] RoomIds => _roomIds;

		/// <summary>
		/// 弹幕客户端
		/// </summary>
		public TcpClient[] Clients => _clients;

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="roomIds">要监控的房间ID数组</param>
		public DanmuMonitor(uint[] roomIds) {
			if (roomIds == null)
				throw new ArgumentNullException(nameof(roomIds));

			_roomIds = roomIds;
			_clients = new TcpClient[_roomIds.Length];
			for (int i = 0; i < _clients.Length; i++)
				_clients[i] = new TcpClient();
			_isConnecteds = new bool[_roomIds.Length];
			_connectionKeeper = new Thread(() => SafeCaller.Loop(KeepConnection)) {
				IsBackground = true,
				Name = "DanmuMonitor.ConnectionKeeper"
			};
			_heartBeatSender = new Thread(() => SafeCaller.Loop(SendHeartBeat)) {
				IsBackground = true,
				Name = "DanmuMonitor.HeartBeatSender"
			};
			_danmuHandler = new Thread(() => SafeCaller.Loop(HandleDanmu)) {
				IsBackground = true,
				Name = "DanmuMonitor.DanmuHandler"
			};
			_cachedCheckRead = new List<Socket>(_clients.Length);
		}

		/// <summary>
		/// 启动
		/// </summary>
		public void Start() {
			if (_isDisposed)
				throw new ObjectDisposedException("弹幕监视器已被Dispose，若要重新启动，请重新实例化DanmuMonitor");

			_connectionKeeper.Start();
			_danmuHandler.Start();
		}

		private void KeepConnection() {
			List<TcpClient> unconnectedClients;
			int[] unconnectedClientIndexMap;

			unconnectedClients = new List<TcpClient>(_clients.Length);
			unconnectedClientIndexMap = new int[_clients.Length];
			while (true) {
				int connectedCount;

				for (int i = 0; i < _clients.Length; i++)
					if (!_isConnecteds[i]) {
						unconnectedClientIndexMap[unconnectedClients.Count] = i;
						// 将TcpClient在unconnectedClients中的索引转换到_clients中的索引
						unconnectedClients.Add(_clients[i]);
					}
				if (unconnectedClients.Count == 0)
					// 所有客户端都连接到了服务器
					goto sleep;
				GlobalSettings.Logger.LogInfo($"检测到 {unconnectedClients.Count} 个客户端处于离线状态");
				connectedCount = 0;
				if (unconnectedClients.Count > 50)
					// 如果未连接客户端过多就并行处理
					Parallel.For(0, unconnectedClients.Count, i => {
						if (SafeCaller.Call(() => ConnectAndEnterRoom(unconnectedClients, i), true)) {
							Interlocked.Increment(ref connectedCount);
							_isConnecteds[unconnectedClientIndexMap[i]] = true;
						}
					});
				else
					for (int i = 0; i < unconnectedClients.Count; i++)
						if (SafeCaller.Call(() => ConnectAndEnterRoom(unconnectedClients, i), true)) {
							connectedCount++;
							_isConnecteds[unconnectedClientIndexMap[i]] = true;
						}
				GlobalSettings.Logger.LogInfo($"{connectedCount} 个客户端成功连接到弹幕服务器");
				unconnectedClients.Clear();
			sleep:
				Thread.Sleep(1000);
				// 1秒检查一次 TODO: 延时时间加入配置文件
			}
		}

		private void ConnectAndEnterRoom(List<TcpClient> unconnectedClients, int i) {
			DanmuApi.Connect(unconnectedClients[i]);
			DanmuApi.EnterRoom(unconnectedClients[i], _roomIds[i]);
			DanmuApi.SendHeartBeat(unconnectedClients[i]);
		}

		private void SendHeartBeat() {
			throw new NotImplementedException();
		}

		private void HandleDanmu() {
			while (true) {
				foreach (Danmu danmu in EnumerateDanmus()) {
					// TODO
					Console.WriteLine(danmu);
				}
			}
		}

		private IEnumerable<Danmu> EnumerateDanmus() {
			while (true) {
				_cachedCheckRead.Clear();
				for (int i = 0; i < _clients.Length; i++)
					if (_isConnecteds[i])
						// 只需要检查已连接的客户端
						_cachedCheckRead.Add(_clients[i].Client);
				if (_cachedCheckRead.Count == 0)
					// 此时没有任何连接到服务器的客户端（比如刚刚调用Start方法的时候），checkRead为空会报错
					goto sleep;
				Socket.Select(_cachedCheckRead, null, null, 1000);
				if (_cachedCheckRead.Count == 0)
					// 说明没有收到弹幕的客户端
					goto sleep;
				break;
			sleep:
				Thread.Sleep(1000);
				// TODO: 延时时间加入配置文件
			}
			return _cachedCheckRead.Select(socket => DanmuApi.ResolveDanmu(socket)).Where(danmu => danmu != Danmu.Empty);
		}

		/// <summary />
		public void Dispose() {
			if (_isDisposed)
				return;
			foreach (TcpClient client in _clients)
				client.Dispose();
			_isDisposed = true;
		}
	}
}
