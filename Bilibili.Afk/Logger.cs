using System;
using Bilibili.Api;

namespace Bilibili.Afk {
	/// <summary />
	public sealed class Logger : ILogger {
		private static readonly Logger _instance = new Logger();
		private static readonly object _syncRoot = new object();

		public static Logger Instance => _instance;

		private Logger() {
		}

		/// <summary />
		public void LogNewLine() {
			lock (_syncRoot)
				Console.WriteLine();
		}

		/// <summary />
		public void LogInfo(string value) {
			lock (_syncRoot)
				Console.WriteLine(value);
		}

		/// <summary />
		public void LogWarning(string value) {
			lock (_syncRoot) {
				ConsoleColor color;

				color = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(value);
				Console.ForegroundColor = color;
			}
		}

		/// <summary />
		public void LogError(string value) {
			lock (_syncRoot) {
				ConsoleColor color;

				color = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Error.WriteLine(value);
				Console.ForegroundColor = color;
			}
		}
	}
}
