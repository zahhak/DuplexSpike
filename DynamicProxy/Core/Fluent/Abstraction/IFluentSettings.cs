using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Telerik.DynamicProxy.Fluent.Abstraction
{
    /// <summary>
    /// Defines fluent memeber for defining the proxy.
    /// </summary>
    public interface IFluentSettings
    {
        /// <summary>
        /// Define the interceptor to hook.
        /// </summary>
        /// <param name="interceptor"></param>
        /// <returns></returns>
        IFluentSettings Register(DynamicProxy.Abstraction.IInterceptor interceptor);
        
        /// <summary>
        /// Pass the constructor that will be invoked during proxy creation.
        /// </summary>
        /// <returns></returns>
        IFluentSettings CallConstructor(params object[] args);

        /// <summary>
        /// Defines that object overrides like "Equals", "ToString", "GetHashCode" should
        /// be ignored.
        /// </summary>
        /// <returns></returns>
        IFluentSettings IncludeObjectOverrides();

        /// <summary>
        /// Implements the specified interface.
        /// </summary>
        /// <param name="interface">Type of target interface.</param>
        /// <returns>Referece to <see cref="IFluentSettings"/></returns>
        IFluentSettings Implement(Type @interface);
    }
}
