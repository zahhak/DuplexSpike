using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Telerik.DynamicProxy;

namespace Telerik.DynamicProxy.Abstraction
{
    /// <summary>
    /// Interface describing method invocation details.
    /// </summary>
    public interface IInvocation : IReturnValueInvocation
    {
        /// <summary>
        /// Gets the declaring type of the invocation.
        /// </summary>
        Type DeclaringType { get; }
        /// <summary>
        /// Gets the invocation target
        /// </summary>
        object Target { get; }
        /// <summary>
        /// Gets the arguments for invoked method.
        /// </summary>
        Argument[] Arguments { get; }
  
        /// <summary>
        /// Sets the return value for the interception.
        /// </summary>
        /// <param name="value"></param>
        void SetReturn(object value);
        /// <summary>
        /// Gets the invoked member itself.
        /// </summary>
        MethodInfo Method { get; }
        /// <summary>
        /// Exectues the orignal method.
        /// </summary>
        void Continue();
    }
}
