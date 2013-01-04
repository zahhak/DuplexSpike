 using System;
using System.Threading.Tasks;

namespace SignalRClient
{
	public interface IOperationExecutor<TOperation>
	{
		Task Enqueue(Action<TOperation> setup);

		Task<TResult> Enqueue<TResult>(Func<TOperation, TResult> setup);
	}
}