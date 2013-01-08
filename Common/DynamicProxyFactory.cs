using System;
using System.Linq;
using System.Reactive.Subjects;
using Telerik.DynamicProxy;

namespace Common
{
	public static class DynamicProxyFactory
	{
		public static TProxyInterface CreateProxy<TProxyInterface, TTargetObject>(params object[] proxyConstructorArguments)
		{
			var factory = new ProxyFactory(typeof(ProxyBase<TTargetObject>));
			factory.Implement(typeof(TProxyInterface));
			factory.Register(new OperationDescriptorInterceptor<TTargetObject>());
			factory.CallingConstructor = new Ctor(proxyConstructorArguments);
			return (TProxyInterface)factory.Create();
		}
	}
}
