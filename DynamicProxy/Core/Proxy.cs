using System;
using Telerik.DynamicProxy.Abstraction;
using Telerik.DynamicProxy.Fluent;
using Telerik.DynamicProxy.Fluent.Abstraction;

namespace Telerik.DynamicProxy
{
    /// <summary>
    /// Entry-point for creating proxy from a given type.
    /// </summary>
    public class Proxy
    {
        internal Proxy(Type target, bool incldueObjectOverrides, bool mockedConstructor)
        {
            typeEmitter = new TypeEmitter(target, incldueObjectOverrides, mockedConstructor);
        }

        /// <summary>
        /// Gets or set the array for interfaces to implement.
        /// </summary>
        internal Type[] InterfacesToImplement
        {
            get;
            set;
        }

        ///<summary>
        /// Creates the proxy for the specified interface.
        ///</summary>
        ///<param name="interceptor"></param>
        ///<param name="args"></param>
        ///<typeparam name="T"></typeparam>
        ///<returns></returns>
        public static T Create<T>(IInterceptor interceptor, params object[] args)
        {
            return (T)Create(typeof(T), interceptor, args);
        }

        /// <summary>
        /// Creates a new proxy from the specified type
        /// </summary>
        /// <param name="target"></param>
        /// <param name="interceptor"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object Create(Type target, IInterceptor interceptor, params object[] args)
        {
            ProxyFactory factory = new ProxyFactory(target);

            factory.Register(interceptor);
            factory.CallingConstructor = new Ctor(args);

            return factory.Create();
        }

        /// <summary>
        /// Creates a new proxy with specific settings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Create<T>(Action<IFluentSettings> action)
        {
            return (T)Create(typeof(T), action);
        }

        /// <summary>
        /// Creates a new proxy with specific settings.
        /// </summary>
        /// <returns></returns>
        public static object Create(Type target, Action<IFluentSettings> action)
        {
            var proxy = new FluentProxy(target);

            action(proxy);

            return proxy.NewInstance();
        }

        internal Type CreateType()
        {
            typeEmitter.Emit(this.InterfacesToImplement);
            return typeEmitter.CreateType();
        }

        private readonly TypeEmitter typeEmitter;
    }
}
