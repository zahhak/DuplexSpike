using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.DynamicProxy.Abstraction;

namespace Common
{
	public static class InvocationExtensions
	{
		public static IOperationDescriptor GetOperationDescriptor(this IInvocation invocation)
		{
			var parameters = invocation.Method
									   .GetParameters()
									   .Zip(invocation.Arguments, (parameterInfo, argument) => new
									   {
										   ParameterName = parameterInfo.Name,
										   Value = argument.Value
									   })
									   .ToDictionary(pair => pair.ParameterName, pair => pair.Value);

			return new OperationDescriptor
			{
				Parameters = parameters,
				MethodName = invocation.Method.Name
			};
		}
	}
}
