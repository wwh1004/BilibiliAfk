using System;

namespace Bilibili.Api {
	internal static class Logger {
		public static void LogNewLine() {
			Console.WriteLine();
		}

		public static void LogInfo(string value) {
			Console.WriteLine(value);
		}

		public static void LogWarning(string value) {
			ConsoleColor color;

			color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(value);
			Console.ForegroundColor = color;
		}

		public static void LogError(string value) {
			ConsoleColor color;

			color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Error.WriteLine(value);
			Console.ForegroundColor = color;
		}
	}
}
