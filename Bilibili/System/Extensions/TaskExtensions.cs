using System.Threading;
using System.Threading.Tasks;

namespace System.Extensions {
	internal static class TaskExtensions {
		public static async Task WithCancellation(this Task task, CancellationToken cancellationToken) {
			TaskCompletionSource<bool> taskCompletionSource;

			taskCompletionSource = new TaskCompletionSource<bool>();
			using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), taskCompletionSource))
				if (task != await Task.WhenAny(task, taskCompletionSource.Task))
					throw new OperationCanceledException(cancellationToken);
			await task;
		}

		public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken) {
			TaskCompletionSource<bool> taskCompletionSource;

			taskCompletionSource = new TaskCompletionSource<bool>();
			using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), taskCompletionSource))
				if (task != await Task.WhenAny(task, taskCompletionSource.Task))
					throw new OperationCanceledException(cancellationToken);
			return await task;
		}
	}
}
