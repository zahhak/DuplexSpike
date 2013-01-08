using System;
using Telerik.DynamicProxy.Abstraction;
using Telerik.DynamicProxy.Fluent.Abstraction;

namespace Telerik.DynamicProxy.Fluent
{
    /// <summary>
    /// Fluent proxy
    /// </summary>
    public class FluentProxy : IFluentSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FluentProxy"/> class.
        /// </summary>
        /// <param name="target"></param>
        public FluentProxy(Type target)
        {
            factory = new ProxyFactory(target);
        }

        /// <summary>
        /// Define the interceptor to hook.
        /// </summary>
        /// <param name="interceptor"></param>
        /// <returns></returns>
        public IFluentSettings Register(IInterceptor interceptor)
        {
            factory.Register(interceptor);
            return this;
        }

        /// <summary>
        /// Setups the target constructor to be called.
        /// </summary>
        public IFluentSettings CallConstructor(params object[] args)
        {
            factory.CallingConstructor = new Ctor(args);
            return this;
        }

#if !SILVERLIGHT

        /// <summary>
        /// Setups the target default constructor to be called.
        /// </summary>
        public IFluentSettings CallConstructor(bool mocked)
        {
            factory.CallingConstructor = new Ctor(mocked);
            return this;
        }

#endif

        /// <summary>
        /// Defines that object overrides like "Equals", "ToString", "GetHashCode" should
        /// be ignored.
        /// </summary>
        /// <returns></returns>
        public IFluentSettings IncludeObjectOverrides()
        {
            factory.IncludeObjectOverrides();
            return this;
        }

        /// <summary>
        /// Implements the specified interface.
        /// </summary>
        /// <param name="interface">Type of target interface.</param>
        /// <returns>Referece to <see cref="IFluentSettings"/></returns>
        public IFluentSettings Implement(Type @interface)
        {
            factory.Implement(@interface);
            return this;
        }


        /// <summary>
        /// Creates the new instance.
        /// </summary>
        /// <returns></returns>
        public object NewInstance()
        {
            return factory.Create();
        }

        private readonly ProxyFactory factory;
    }
}
