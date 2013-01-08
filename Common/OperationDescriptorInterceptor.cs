using System;
using System.Linq;
using System.Threading.Tasks;
using Telerik.DynamicProxy.Abstraction;

namespace Common
{
	public class OperationDescriptorInterceptor<T> : IInterceptor
	{
		public virtual void Intercept(IInvocation invocation)
		{
			var target = (ProxyBase<T>)invocation.Target;

			dynamic taskCompletionSource = GetTaskCompletionSource(invocation);
	
			target.SendAsync(invocation.GetOperationDescriptor(), taskCompletionSource);

			invocation.SetReturn(taskCompletionSource.Task);
		}
  
		private static dynamic GetTaskCompletionSource(IInvocation invocation)
		{
			if (typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
			{
				var innerType = invocation.Method.ReturnType.GetGenericArguments().SingleOrDefault();
				if (innerType == null)
				{
					innerType = typeof(object);
				}

				var taskCompletionSourceType = typeof(TaskCompletionSource<>).MakeGenericType(innerType);
				return Activator.CreateInstance(taskCompletionSourceType);
			}
			else
			{
				throw new InvalidOperationException(string.Format("Unsupported Return Type: {0}", invocation.Method.ReturnType));
			}
		}
	}
}