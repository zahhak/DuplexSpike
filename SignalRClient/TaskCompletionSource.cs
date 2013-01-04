using System;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRClient
{
	public class TaskCompletionSource
	{
		private readonly TaskCompletionSource<object> innerTaskCompletionSource;

		public static readonly Task CompletedTask = New<object>(null);
		public static readonly Task CanceledTask = NewCanceled<object>();

		public Task Task
		{
			get
			{
				return this.innerTaskCompletionSource.Task;
			}
		}

		public TaskCompletionSource()
		{
			this.innerTaskCompletionSource = new TaskCompletionSource<object>();
		}

		public void SetCanceled()
		{
			this.innerTaskCompletionSource.TrySetCanceled();
		}

		public void SetException(Exception ex)
		{
			this.innerTaskCompletionSource.TrySetException(ex);
		}

		public void SetResult()
		{
			this.innerTaskCompletionSource.TrySetResult(null);
		}

		public static Task<TResult> New<TResult>(TResult result)
		{
			var taskCompletionSource = new TaskCompletionSource<TResult>();
			taskCompletionSource.SetResult(result);

			return taskCompletionSource.Task;
		}

		public static Task New()
		{
			var taskCompletionSource = new TaskCompletionSource();
			taskCompletionSource.SetResult();

			return taskCompletionSource.Task;
		}

		public static Task<TResult> NewCanceled<TResult>()
		{
			var taskCompletionSource = new TaskCompletionSource<TResult>();
			taskCompletionSource.SetCanceled();

			return taskCompletionSource.Task;
		}

		public static Task<TResult> NewCanceled<TResult>(CancellationToken token)
		{
			return new Task<TResult>(() => default(TResult), token);
		}

		public static Task<TResult> NewFaulted<TResult>(Exception error)
		{
			var taskCompletionSource = new TaskCompletionSource<TResult>();
			taskCompletionSource.SetException(error);

			return taskCompletionSource.Task;
		}

		public static Task NewFaulted(Exception error)
		{
			var taskCompletionSource = new TaskCompletionSource();
			taskCompletionSource.SetException(error);

			return taskCompletionSource.Task;
		}
	}
}
