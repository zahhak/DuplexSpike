using System;

namespace Common
{
	public interface IOperationSession<TCallback> : IDisposable
	{
		void Initialize(TCallback callback);
	}
}