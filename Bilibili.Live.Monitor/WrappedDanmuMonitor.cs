using System;
using System.Threading;
using System.Threading.Tasks;
using Bilibili.Settings;

namespace Bilibili.Live.Monitor {
	/// <summary>
	/// 包装后的 <see cref="DanmuMonitor"/>，保证在 <see cref="DanmuMonitor"/> 实例出错后自动创建新实例
	/// </summary>
	public sealed class WrappedDanmuMonitor : IDisposable {
		private readonly Func<DanmuMonitor> _creator;
		private readonly CancellationTokenSource _manualCts;
		private readonly object _syncRoot;
		private DanmuMonitor _value;
		private bool _isDisposed;

		/// <summary>
		/// 当前的 <see cref="DanmuMonitor"/> 实例
		/// </summary>
		public DanmuMonitor Value => _value;

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="creator"><see cref="DanmuMonitor"/> 实例创建器</param>
		public WrappedDanmuMonitor(Func<DanmuMonitor> creator) {
			if (creator == null)
				throw new ArgumentNullException(nameof(creator));

			_creator = creator;
			_manualCts = new CancellationTokenSource();
			_syncRoot = new object();
		}

		/// <summary />
		public void Execute() {
			lock (_syncRoot) {
				if (_isDisposed)
					return;
				ExecuteNoLock();
			}
		}

		private void ExecuteNoLock() {
			_value = _creator();
			_ = _value.ExecuteLoopAsync().ContinueWith(Continue, _manualCts.Token);
		}

		private void Continue(Task task) {
			lock (_syncRoot) {
				if (_isDisposed)
					return;
				GlobalSettings.Logger.LogError($"{_value.Id} 号弹幕监控与服务器的连接意外断开");
				_value.Dispose();
				ExecuteNoLock();
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
	}
}
