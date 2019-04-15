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
	public sealed class DanmuMonitor:IDisposable {
		private readonly uint[] _roomIds;
		private readonly TcpClient[] _clients;
		private readonly bool[] _isConnecteds;
		private readonly Thread _connectionKeeper;
		private readonly List<Socket> _cachedCheckRead;
		private readonly Thread _danmuHandler;
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
			_connectionKeeper = new Thread(() => SafeCaller.Call(KeepConnection, false, -1, 0)) {
				IsBackground = true,
				Name = "DanmuMonitor.ConnectionKeeper"
			};
			_cachedCheckRead = new List<Socket>(_clients.Length);
			_danmuHandler = new Thread(() => SafeCaller.Call(HandleDanmu, false, -1, 0)) {
				IsBackground = true,
				Name = "DanmuMonitor.DanmuHandler"
			};
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

		private List<TcpClient> GetReadableClients() {
			List<TcpClient> checkRead;

			checkRead = new List<TcpClient>(_clients.Length);
			for (int i = 0; i < _clients.Length; i++)
				if (!_isConnecteds[i])
					checkRead.Add(_clients[i]);
			Socket.Select(checkRead, null, null, -1);
			return checkRead;
		}

		private void KeepConnection() {
			List<TcpClient> unconnectedClients;

			unconnectedClients = new List<TcpClient>(_clients.Length);
			while (true) {
				int connectedCount;

				for (int i = 0; i < _clients.Length; i++)
					if (!_isConnecteds[i])
						unconnectedClients.Add(_clients[i]);
				GlobalSettings.Logger.LogInfo($"检测到 {unconnectedClients.Count} 个客户端处于离线状态");
				connectedCount = 0;
				if (unconnectedClients.Count > 50)
					// 如果未连接客户端过多就并行处理
					Parallel.For(0, unconnectedClients.Count, i => {
						if (SafeCaller.Call(() => DanmuApi.Connect(unconnectedClients[i]), true))
							Interlocked.Increment(ref connectedCount);
					});
				else
					foreach (TcpClient client in unconnectedClients)
						if (SafeCaller.Call(() => DanmuApi.Connect(client), true))
							connectedCount++;
				GlobalSettings.Logger.LogInfo($"{connectedCount} 个客户端成功连接到弹幕服务器");
				unconnectedClients.Clear();
				Thread.Sleep(1000);
				// 1秒检查一次 TODO: 延时时间加入配置文件
			}
		}

		private void HandleDanmu() {
			foreach (Danmu danmu in EnumerateDanmus()) {
				// TODO
			}
		}

		private IEnumerable<Danmu> EnumerateDanmus() {
			while (true) {
				_cachedCheckRead.Clear();
				for (int i = 0; i < _clients.Length; i++)
					if (_isConnecteds[i])
						// 只需要检查已连接的客户端
						_cachedCheckRead.Add(_clients[i].Client);
				Socket.Select(_cachedCheckRead, null, null, 1000);
				if (_cachedCheckRead.Count == 0)
					Thread.Sleep(1000);
				// TODO: 延时时间加入配置文件
				else
					break;
			}
			return _cachedCheckRead.Select(socket => {
				return new Danmu(); // NOT IMPL
			});
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
