using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.DynamicProxy.Abstraction;
using System.Reflection;

namespace Telerik.DynamicProxy
{
    /// <summary>
    /// Proxy factory.
    /// </summary>
    public class ProxyFactory : IFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyFactory"/> class.
        /// </summary>
        public ProxyFactory(Type type)
        {
            settings = new ProxySettings(type);
        }

        /// <summary>
        /// Gets/Sets the initializing contructor for the proxy.
        /// </summary>
        public Ctor CallingConstructor
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies to include object overrides in the target proxy.
        /// </summary>
        public void IncludeObjectOverrides()
        {
            settings.IncludeObjectOverrides = true;
        }

        /// <summary>
        /// Appends a new interceptor to the invocation list.
        /// </summary>
        /// <param name="interceptor"></param>
        public void Register(IInterceptor interceptor)
        {
            settings.AddInterceptor(interceptor);
        }

        /// <summary>
        /// Implements the specified interface to the target proxy.
        /// </summary>
        /// <param name="interface"></param>
        public void Implement(Type @interface)
        {
            settings.Interfaces.Add(@interface);
        }

        ///<summary>
        /// Creates the new instance of the proxy.
        ///</summary>
        ///<returns></returns>
        public object Create()
        {
            var args = new object[0];

            bool skipBaseCtor = false;

            if (CallingConstructor != null)
            {
                args = CallingConstructor.ToArgArray();
                skipBaseCtor = CallingConstructor.Proxied;
                settings.SkipBaseConstructor = skipBaseCtor;
            }

            Type target = settings.Target;
            int hashCode = settings.GetHashCode();

            if (!cache.ContainsKey(hashCode))
            {
                var proxy = new Proxy(target, settings.IncludeObjectOverrides, skipBaseCtor)
                {
                    InterfacesToImplement = settings.Interfaces.ToArray()
                };

                cache.Add(hashCode, proxy.CreateType());
            }
            Type proxyType = cache[hashCode];

            object[] extArgs = ExtendWithInterceptor(args, settings.ToInterceptorArray());

            if (proxyType.ContainsGenericParameters)
            {
                Type[] genArgs = target.GetGenericArguments();
                proxyType = proxyType.MakeGenericType(genArgs);
            }

            return CreateInstance(proxyType, extArgs);
        }

        private static object CreateInstance(Type proxyType, object[] extArgs)
        {
            try
            {
#if !SILVERLIGHT
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                return Activator.CreateInstance(proxyType, flags, null, extArgs, null);
#else
                return Activator.CreateInstance(proxyType, extArgs);
#endif
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        internal static object[] ExtendWithInterceptor(object[] args, object[] interceptors)
        {
            var extendedArgs = new object[args.Length + 1];

            extendedArgs[0] = interceptors;
            args.CopyTo(extendedArgs, 1);

            return extendedArgs;
        }

        private ProxySettings settings;
        private static IDictionary<int, Type> cache = new Dictionary<int, Type>();
    }

    ///<summary>
    /// Factory class for creating the proxy.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class ProxyFactory<T> : ProxyFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyFactory{T}"/> class.
        /// </summary>
        public ProxyFactory()
            : base(typeof(T))
        {
        }

        /// <summary>
        /// Creates a new proxy from with the defined settings.
        /// </summary>
        /// <returns></returns>
        public new T Create()
        {
            return (T)base.Create();
        }
    }
}
