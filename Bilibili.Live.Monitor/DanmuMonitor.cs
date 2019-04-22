using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Bilibili.Api;
using Bilibili.Settings;

namespace Bilibili.Live.Monitor {
	/// <summary />
	public class DanmuHandlerEventArgs : EventArgs {
		private readonly Danmu _danmu;

		/// <summary />
		public Danmu Danmu => _danmu;

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="danmu"></param>
		public DanmuHandlerEventArgs(Danmu danmu) {
			if (danmu == null)
				throw new ArgumentNullException(nameof(danmu));

			_danmu = danmu;
		}
	}

	/// <summary>
	/// 弹幕监控器
	/// </summary>
	public sealed class DanmuMonitor : IDisposable {
		private readonly uint _roomId;
		private readonly TcpClient _client;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private int? _id;
		private bool _isDisposed;

		private static readonly TimeSpan _receiveTimeout = DanmuApi.HeartBeatInterval + new TimeSpan(0, 0, 10);

		/// <summary>
		/// 房间ID
		/// </summary>
		public uint RoomId => _roomId;

		/// <summary>
		/// 弹幕客户端
		/// </summary>
		public TcpClient Client => _client;

		/// <summary>
		/// 监控器ID
		/// </summary>
		public int? Id {
			get => _id;
			set => _id = value;
		}

		/// <summary>
		/// 在接收到可能的抽奖弹幕时发生
		/// </summary>
		public event EventHandler<DanmuHandlerEventArgs> DanmuHandler;

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="roomId">要监控的房间ID</param>
		public DanmuMonitor(uint roomId) {
			_roomId = roomId;
			_client = new TcpClient();
			_cancellationTokenSource = new CancellationTokenSource();
		}

		/// <summary>
		/// 执行
		/// </summary>
		public async Task ExecuteAsync() {
			if (_isDisposed)
				throw new ObjectDisposedException("弹幕监控器已被Dispose，若要重新启动，请重新实例化DanmuMonitor");

			await DanmuApi.ConnectAsync(_client);
			await DanmuApi.EnterRoomAsync(_client, _roomId);
			await DanmuApi.SendHeartBeatAsync(_client);
			// 先发送一次心跳，因为不确定HeartBeatManager什么时候会再次批量发送心跳
			HeartBeatManager.Instance.Add(_client);
			// 让HeartBeatManager接管心跳
			await ExecuteLoopAsync();
		}

		private async Task ExecuteLoopAsync() {
			while (true) {
				Danmu danmu;

				try {
					CancellationToken cancellationToken;

					cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(_receiveTimeout).Token, _cancellationTokenSource.Token).Token;
					danmu = await DanmuApi.ReadDanmuAsync(_client, cancellationToken);
				}
				catch (OperationCanceledException ex) {
					if (ex.CancellationToken != _cancellationTokenSource.Token) {
						GlobalSettings.Logger.LogError($"{_id} 号弹幕监控与服务器的连接意外断开");
						Dispose();
					}
					return;
				}
				catch (Exception o) {
					GlobalSettings.Logger.LogException(o);
					// 不知道为什么不捕获异常
					throw o;
				}
				switch (danmu.Type) {
				case DanmuType.Command:
					DanmuHandler?.Invoke(this, new DanmuHandlerEventArgs(danmu));
					break;
				case DanmuType.Handshaking:
					if (_id != null)
						GlobalSettings.Logger.LogInfo($"{_id} 号弹幕监控进入房间 {_roomId}");
					break;
				default:
					break;
				}
			}
		}

		/// <summary />
		public void Dispose() {
			if (_isDisposed)
				return;
			_cancellationTokenSource.Cancel();
			HeartBeatManager.Instance.Remove(_client);
			_client.Dispose();
			_isDisposed = true;
		}
	}
}
