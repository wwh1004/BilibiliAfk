using System;

namespace Bilibili.Api {
	/// <summary>
	/// 应该暂停操作一段时间，暂停时间视情况而定
	/// </summary>
	public sealed class ShouldSleepException : Exception {
	}
}
