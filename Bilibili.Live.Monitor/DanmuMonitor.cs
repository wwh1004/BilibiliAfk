using System;
using System.Extensions;
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
		private readonly int _id;
		private readonly bool _showHeartBeat;
		private readonly CancellationTokenSource _manualCts;
		private readonly object _syncRoot;
		private DanmuMonitorImpl _value;
		private bool _isDisposed;

		/// <summary>
		/// 房间ID
		/// </summary>
		public uint RoomId => _roomId;

		/// <summary>
		/// 监控器ID
		/// </summary>
		public int Id => _id;

		/// <summary>
		/// 是否显示心跳，默认为否
		/// </summary>
		public bool ShowHeartBeat => _showHeartBeat;

		/// <summary>
		/// 在接收到可能的抽奖弹幕时发生
		/// </summary>
		public event EventHandler<DanmuHandlerEventArgs> DanmuHandler;

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="roomId">要监控的房间ID</param>
		/// <param name="id"></param>
		/// <param name="showHeartBeat"></param>
		public DanmuMonitor(uint roomId, int id, bool showHeartBeat) {
			_roomId = roomId;
			_id = id;
			_showHeartBeat = showHeartBeat;
			_manualCts = new CancellationTokenSource();
			_syncRoot = new object();
		}

		/// <summary />
		public void Execute() {
			lock (_syncRoot) {
				CreateDanmuMonitorImpl();
				_ = _value.ExecuteLoopAsync();
			}
		}

		private void CreateDanmuMonitorImpl() {
			_value = new DanmuMonitorImpl(_roomId, _id, _showHeartBeat, _manualCts.Token);
			_value.DanmuHandler += (object sender, DanmuHandlerEventArgs e) => DanmuHandler?.Invoke(sender, e);
			_value.Disposing += (object sender, EventArgs e) => {
				GlobalSettings.Logger.LogError($"{_id} 号弹幕监控与服务器的连接意外断开，3s后尝试重连");
				Restart(new TimeSpan(0, 0, 3));
			};
		}

		private void Restart(TimeSpan delay) {
			lock (_syncRoot) {
				CreateDanmuMonitorImpl();
				_ = _value.ExecuteLoopAsync(delay);
			}
		}

		/// <summary />
		public void Dispose() {
			lock (_syncRoot) {
				if (_isDisposed)
					return;
				_manualCts.Cancel();
				_manualCts.Dispose();
				_value.Dispose();
				_value = null;
				_isDisposed = true;
			}
		}

		private sealed class DanmuMonitorImpl : IDisposable {
			private readonly uint _roomId;
			private readonly int _id;
			private readonly bool _showHeartBeat;
			private readonly CancellationToken _cancellationToken;
			private readonly TcpClient _client;
			private readonly CancellationTokenSource _manualCts;
			private bool _isDisposed;

			private static readonly TimeSpan _receiveTimeout = DanmuApi.HeartBeatInterval + new TimeSpan(0, 0, 10);

			public event EventHandler<DanmuHandlerEventArgs> DanmuHandler;

			public event EventHandler Disposing;

			public DanmuMonitorImpl(uint roomId, int id, bool showHeartBeat, CancellationToken cancellationToken) {
				_roomId = roomId;
				_id = id;
				_showHeartBeat = showHeartBeat;
				_cancellationToken = cancellationToken;
				_client = new TcpClient();
				_manualCts = new CancellationTokenSource();
			}

			public async Task ExecuteLoopAsync() {
				await ExecuteLoopAsync(TimeSpan.Zero);
			}

			public async Task ExecuteLoopAsync(TimeSpan delay) {
				if (_isDisposed)
					throw new ObjectDisposedException($"弹幕监控器已被Dispose，若要重新启动，请重新实例化{nameof(DanmuMonitorImpl)}");

				await Task.Delay(delay);
				await DanmuApi.ConnectAsync(_client);
				await DanmuApi.EnterRoomAsync(_client, _roomId);
				_ = ExecuteHeartBeatLoopImplAsync();
				await ExecuteLoopImplAsync();
				Dispose();
			}

			private async Task ExecuteLoopImplAsync() {
				while (true) {
					Danmu danmu;

					if (_isDisposed)
						return;
					try {
						using (CancellationTokenSource timeoutCts = new CancellationTokenSource(_receiveTimeout))
						using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _manualCts.Token, _cancellationToken))
							danmu = await DanmuApi.ReadDanmuAsync(_client).WithCancellation(linkedCts.Token);
					}
					catch (OperationCanceledException) {
						return;
					}
					catch (Exception ex) {
						if (!_isDisposed)
							// 可能是资源释放不同步导致错误，不记录
							GlobalSettings.Logger.LogException(ex);
						return;
					}
					if (_isDisposed)
						return;
					switch (danmu.Type) {
					case DanmuType.Command:
						try {
							DanmuHandler?.Invoke(this, new DanmuHandlerEventArgs(danmu));
						}
						catch (Exception ex) {
							if (!_isDisposed)
								GlobalSettings.Logger.LogException(ex);
						}
						break;
					case DanmuType.Handshaking:
						GlobalSettings.Logger.LogInfo($"{_id} 号弹幕监控进入房间 {_roomId}");
						break;
					}
				}
			}

			private async Task ExecuteHeartBeatLoopImplAsync() {
				while (true) {
					DateTime startTime;
					TimeSpan span;

					startTime = DateTime.Now;
					if (_isDisposed)
						return;
					try {
						await DanmuApi.SendHeartBeatAsync(_client);
						if (_showHeartBeat)
							GlobalSettings.Logger.LogInfo($"{_id} 号弹幕监控已发送心跳");
					}
					catch (Exception ex) {
						if (!_isDisposed)
							GlobalSettings.Logger.LogException(ex);
						return;
					}
					if (_isDisposed)
						return;
					span = DanmuApi.HeartBeatInterval - (DateTime.Now - startTime);
					if (span.Ticks > 0)
						await Task.Delay(span);
				}
			}

			public void Dispose() {
				if (_isDisposed)
					return;
				Disposing?.Invoke(this, EventArgs.Empty);
				_manualCts.Cancel();
				_manualCts.Dispose();
				_client.Dispose();
				_isDisposed = true;
			}
		}
	}
}
