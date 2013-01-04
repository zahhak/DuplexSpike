using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common;
using Telerik.DynamicProxy.Abstraction;

namespace SignalRClient
{
	internal class SessionProxyMethodInterceptor<TOperation, TCallback> : OperationDescriptorInterceptor
	{
		private readonly DuplexOperationExecutor<TOperation, TCallback> executor;

		public SessionProxyMethodInterceptor(DuplexOperationExecutor<TOperation, TCallback> executor)
		{
			this.executor = executor;
		}

		public override void Intercept(IInvocation invocation)
		{
			base.Intercept(invocation);
			if (invocation.Method.ReturnType == typeof(void))
			{
				invocation.SetReturn(executor.Enqueue(this.OperationDescriptor));
			}
			else
			{
				if (typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
				{
					var innerType = invocation.Method.ReturnType.GetGenericArguments().Single();
					var m = executor.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(method => method.Name == "Enqueue" && method.IsGenericMethod);
					var generic = m.MakeGenericMethod(innerType);
					invocation.SetReturn(generic.Invoke(executor, new object[] { this.OperationDescriptor }));
				}
			}
		}
	}
}