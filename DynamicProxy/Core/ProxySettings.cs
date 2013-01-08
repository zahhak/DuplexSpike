using System;
using System.Collections.Generic;
using System.Text;
using Telerik.DynamicProxy.Abstraction;

namespace Telerik.DynamicProxy
{
    internal class ProxySettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyFactory"/> class.
        /// </summary>
        public ProxySettings(Type type)
        {
            this.target = type;
            interceptors = new List<object>();
            interfaces = new List<Type>();
        }

        /// <summary>
        /// Gets lists of interfaces to implement.
        /// </summary>
        public IList<Type> Interfaces
        {
            get
            {
                return interfaces;
            }
        }

        /// <summary>
        /// Gets the target type.
        /// </summary>
        public Type Target
        {
            get
            {
                return target;
            }
        }

        /// <summary>
        /// If set to <value>true</value> 
        /// the proxy will  intercept method overrides like
        /// <example>GetHashCode(), Equals() and ToString()</example>.
        /// </summary>
        public bool IncludeObjectOverrides
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies that proxy should skip the 
        /// base constructor call.
        /// </summary>
        public bool SkipBaseConstructor
        {
            get;
            set;
        }

        /// <summary>
        /// Adds a interceptor
        /// </summary>
        /// <param name="interceptor"></param>
        public void AddInterceptor(object interceptor)
        {
            interceptors.Add(interceptor);
        }

        /// <summary>
        /// Gets the hashCode for the current proxy to be created.
        /// </summary>
        /// <returns>HashCode combining the settings for the proxy.</returns>
        public override int GetHashCode()
        {
            int hashCode = this.target.GetHashCode();

            int index = 0;

            var builder = new StringBuilder();

            // consider ordering and type for interceptor
            foreach (object interceptor in interceptors)
            {
                Type interceptorType = interceptor.GetType();

                builder.Append(interceptorType.Name + index);
                builder.Append(UnderScore);
                builder.Append(interceptorType.GetHashCode());
                builder.Append(UnderScore);
                builder.Append(SkipBaseConstructor);
                builder.Append(UnderScore);

                index++;
            }

            hashCode += builder.ToString().GetHashCode();

            foreach (Type @interface in interfaces)
            {
                hashCode += @interface.GetHashCode();
                index++;
            }

            hashCode += IncludeObjectOverrides.GetHashCode();

            return hashCode;
        }

        internal IInterceptor[] ToInterceptorArray()
        {
            if (arrayOfInterceptors == null)
            {
                arrayOfInterceptors = new IInterceptor[interceptors.Count];
                interceptors.CopyTo(arrayOfInterceptors, 0);
            }
            return arrayOfInterceptors;
        }


        private IInterceptor[] arrayOfInterceptors;
        private readonly IList<object> interceptors;
        private readonly IList<Type> interfaces;
        private readonly Type target;
        private const string UnderScore = "_";
    }
}
