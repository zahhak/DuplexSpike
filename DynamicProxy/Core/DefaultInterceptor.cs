using System;
using Telerik.DynamicProxy.Abstraction;

namespace Telerik.DynamicProxy
{
    /// <summary>
    /// Provides default interceptor implementation.
    /// </summary>
    public class DefaultInterceptor : IInterceptor
    {
        /// <summary>
        /// Interceps method call.
        /// </summary>
        /// <param name="invocation">Wraps the target invocation</param>
        public void Intercept(IInvocation invocation)
        {
            invocation.SetReturn(invocation.Method.ReturnType.GetDefaultValue());
        }
    }
}
