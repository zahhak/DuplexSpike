using System.Linq;
using Telerik.DynamicProxy.Abstraction;

namespace Common
{
	public class OperationDescriptorInterceptor : IInterceptor
	{
		public OperationDescriptor OperationDescriptor { get; private set; }

		public virtual void Intercept(IInvocation invocation)
		{
			var parameters = invocation.Method
										.GetParameters()
										.Zip(invocation.Arguments, (parameterInfo, argument) => new
										{
											ParameterName = parameterInfo.Name,
											Value = argument.Value
										})
										.ToDictionary(pair => pair.ParameterName, pair => pair.Value);
  				
			this.OperationDescriptor = new OperationDescriptor
			{
				Parameters = parameters,
				MethodName = invocation.Method.Name
			};
		}
	}
}