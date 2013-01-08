using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Telerik.DynamicProxy.Abstraction
{
    /// <summary>
    /// Interface containing proxy settings elements.
    /// </summary>
    public interface IFactory
    {
        /// <summary>
        /// Defines the setttings for the target proxy constructor.
        /// </summary>
        Ctor CallingConstructor{get;}
    }
}
