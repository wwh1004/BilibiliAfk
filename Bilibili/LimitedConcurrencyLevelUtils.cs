using System.Threading;
using System.Threading.Tasks;

namespace Bilibili {
	/// <summary />
	public static class LimitedConcurrencyLevelUtils {
		private static readonly TaskScheduler _taskScheduler = new LimitedConcurrencyLevelTaskScheduler(50);
		private static readonly TaskFactory _taskFactory = new TaskFactory(_taskScheduler);
		private static readonly ParallelOptions _parallelOptions = new ParallelOptions {
			CancellationToken = CancellationToken.None,
			MaxDegreeOfParallelism = -1,
			TaskScheduler = _taskScheduler
		};

		/// <summary />
		public static TaskScheduler TaskScheduler => _taskScheduler;

		/// <summary />
		public static TaskFactory TaskFactory => _taskFactory;

		/// <summary />
		public static ParallelOptions ParallelOptions => _parallelOptions;
	}
}
