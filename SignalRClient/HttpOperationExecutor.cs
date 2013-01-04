using System;
using System.Threading.Tasks;
using Common;

namespace SignalRClient
{
	public abstract class HttpOperationExecutor<TOperation> : IOperationExecutor<TOperation>
	{
		private readonly HttpOperationContext context;

		public HttpOperationExecutor(HttpOperationContext context)
		{
			this.context = context;
		}

		public Task Enqueue(Action<TOperation> setup)
		{
			var operationStub = OperationDescriptorHelper.GetDescriptor(setup);

			return this.Enqueue(operationStub);
		}

		protected abstract internal Task Enqueue(OperationDescriptor descriptor);

		public Task<TResult> Enqueue<TResult>(Func<TOperation, TResult> setup)
		{
			var operationStub = OperationDescriptorHelper.GetDescriptor(setup);

			return this.Enqueue<TResult>(operationStub);
		}

		protected abstract internal Task<TResult> Enqueue<TResult>(OperationDescriptor descriptor);
	}
}