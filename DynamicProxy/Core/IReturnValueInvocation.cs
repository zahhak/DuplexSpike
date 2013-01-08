using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Telerik.DynamicProxy
{

    /// <summary>
    /// Internally used for handling return value on invocation.
    /// </summary>
    public interface IReturnValueInvocation
    {
        /// <summary>
        /// Gets the return value.
        /// </summary>
        object ReturnValue
        {
           get;
        }
    }
}
