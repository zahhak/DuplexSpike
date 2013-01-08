using System;
using System.Linq;
using Common;
using Telerik.DynamicProxy.Abstraction;

namespace SignalRClient
{
	public class SessionCreationInterceptor : IInterceptor
	{
		public IOperationDescriptor OperationDescriptor { get; private set; }

		public void Intercept(IInvocation invocation)
		{
			this.OperationDescriptor = invocation.GetOperationDescriptor();
		}
	}
}
