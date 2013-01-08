using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Telerik.DynamicProxy.Abstraction
{
    /// <summary>
    /// Interface defining the entry point for proxy calls.
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// Interceps method call.
        /// </summary>
        /// <param name="invocation"></param>
        void Intercept(IInvocation invocation);
    }
}
