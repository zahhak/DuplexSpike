using System;
using System.Threading.Tasks;
using Common;

namespace Common
{
	public interface IOperationSession : IDisposable
	{
	}

	public interface IOperationSession<TCallback> : IOperationSession
	{
		Task Future { get; }

		void Initialize(TCallback callback);
	}

	public interface IOperationSession<TResult, TCallback> : IOperationSession<TCallback>
	{
		Task<TResult> Future { get; }
	}
}