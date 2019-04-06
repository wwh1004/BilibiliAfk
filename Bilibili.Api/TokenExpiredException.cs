using System;

namespace Bilibili.Api {
	/// <summary>
	/// 在Toekn过期时引发，程序应该停止操作当前用户并且重新登录
	/// </summary>
	public sealed class TokenExpiredException : Exception {
	}
}
