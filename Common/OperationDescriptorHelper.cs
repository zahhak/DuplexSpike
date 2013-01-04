using System;
using System.Linq;
using Telerik.DynamicProxy;

namespace Common
{
	public static partial class OperationDescriptorHelper
	{
		public static OperationDescriptor GetDescriptor<TOperationContract, TResult>(Func<TOperationContract, TResult> func)
		{
			var operationDescriptorInterceptor = new OperationDescriptorInterceptor();
			var contract = GetProxyFactory<TOperationContract>(operationDescriptorInterceptor);
			func(contract);
			return operationDescriptorInterceptor.OperationDescriptor;
		}

		public static OperationDescriptor GetDescriptor<TOperationContract>(Action<TOperationContract> func)
		{
			var operationDescriptorInterceptor = new OperationDescriptorInterceptor();
			var contract = GetProxyFactory<TOperationContract>(operationDescriptorInterceptor);
			func(contract);
			return operationDescriptorInterceptor.OperationDescriptor;
		}

		private static TOperationContract GetProxyFactory<TOperationContract>(OperationDescriptorInterceptor operationDescriptorInterceptor)
		{
			var factory = new ProxyFactory<TOperationContract>();
			factory.CallingConstructor = new Ctor();
			factory.Register(operationDescriptorInterceptor);
			var contract = factory.Create();
			return contract;
		}
	}
}
