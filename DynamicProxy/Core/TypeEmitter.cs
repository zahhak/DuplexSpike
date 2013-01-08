using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using Telerik.DynamicProxy.Abstraction;
using System.IO;
using System.Runtime.CompilerServices;

namespace Telerik.DynamicProxy
{
    internal class TypeEmitter
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeEmitter"/> class.
        /// </summary>
        /// <param name="target">Target type</param>
        /// <param name="incldueObjectOverrides">Specifies to include object overrides</param>
        /// <param name="mockedConstructor">Specifies if the constructor is mocked</param>
        public TypeEmitter(Type target, bool incldueObjectOverrides, bool mockedConstructor)
            : this(target)
        {
            this.incldueObjectOverrides = incldueObjectOverrides;
            this.mockedConstructor = mockedConstructor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeEmitter"/> class.
        /// </summary>
        /// <param name="target">Target type</param>
        public TypeEmitter(Type target)
        {
            this.target = target;

            var assemblyName = new AssemblyName("Telerik.JustMock")
            {
#if !SILVERLIGHT
                KeyPair = GetStrongNameKeyPair(),
#endif
                Version = new Version(1, 0, 0, 0)
            };

            AssemblyBuilder assBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            // define module
            this.module = assBuilder.DefineDynamicModule(assemblyName.Name);
        }

#if !SILVERLIGHT

        private StrongNameKeyPair GetStrongNameKeyPair()
        {
            using (var sr = new BinaryReader(this.GetType().Assembly.GetManifestResourceStream("Telerik.DynamicProxy.keypair.snk")))
            {
                var buff = new byte[sr.BaseStream.Length];

                sr.Read(buff, 0, buff.Length);

                return new StrongNameKeyPair(buff);
            }
        }

#endif

        public void Emit(Type[] userDefinedInterfaces)
        {
            Type proxy = target.IsInterface ? typeof(AutoClass) : target;

            this.typeBuilder = DefineType(target);

            this.interceptors = typeBuilder.DefineField("interceptors", typeof(IInterceptor[]), FieldAttributes.Public);
            this.interceptorIndex = typeBuilder.DefineField("interceptorIndex", typeof(int), FieldAttributes.Private);

            ExtendAllConstructors(proxy);

            RegisterIntercptors(target);
            RegisterInterfaces(userDefinedInterfaces);
        }

        private void RegisterInterfaces(Type[] @interfaces)
        {
            foreach (Type @interface in @interfaces)
            {
                this.typeBuilder.AddInterfaceImplementation(@interface);

                foreach (var parent in @interface.GetInterfaces())
                {
                    RegisterInterceptor(parent);
                }

                RegisterInterceptor(@interface);
            }
        }

        private TypeBuilder DefineType(Type proxyTarget)
        {
            string typeName = String.Format("{0}Proxy+{1}", proxyTarget.Name, Guid.NewGuid().ToString().Replace("-", string.Empty));

            TypeBuilder builder = module.DefineType(typeName, GetClassAttributes());

            if (proxyTarget.IsInterface)
            {
                builder.AddInterfaceImplementation(proxyTarget);
                builder.SetParent(typeof(object));
            }
            else
            {
                builder.SetParent(proxyTarget);
            }

            // add marker interface
            builder.AddInterfaceImplementation(typeof(IProxy));

            return builder;
        }

        private static TypeAttributes GetClassAttributes()
        {
            return TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable;
        }

        private void ExtendAllConstructors(Type workingTarget)
        {
            if (workingTarget != typeof(AutoClass))
            {
                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

                bool silverlight = false;
#if SILVERLIGHT
                    if (workingTarget.IsAbstract){
                        flags |= BindingFlags.NonPublic;
                    }
                    silverlight = true;
#else
                flags |= BindingFlags.NonPublic;
#endif

                ConstructorInfo[] constructors = workingTarget.GetConstructors(flags);

                bool defaultCtorProcessed = false;

                // process all ctor that has permission or accessible
                foreach (ConstructorInfo constructor in constructors)
                {
                    bool skip = constructor.IsAssembly && !IsInternalsVisibleTo(constructor.Module);

                    if (!skip)
                    {
                        if (constructor.GetParameters().Length == 0)
                        {
                            defaultCtorProcessed = true;
                        }
                        ExtendConstructor(constructor);
                    }
                }

                if (!silverlight && !defaultCtorProcessed)
                {
                    ExtendConstructor(typeof(object).GetConstructor(Type.EmptyTypes));
                }
            }
            else
            {
                ExtendConstructor(typeof(object).GetConstructor(Type.EmptyTypes));
            }
        }

        private void ExtendConstructor(ConstructorInfo constructor)
        {
            ParameterInfo[] parameters = constructor.GetParameters();

            Type[] parameterTypes = new Type[parameters.Length + 1];
            Type[] originalParameterTypes = new Type[parameters.Length];

            parameterTypes[0] = typeof(IInterceptor[]);

            for (int index = 0; index < parameters.Length; index++)
            {
                originalParameterTypes[index]
                    = parameterTypes[index + 1]
                    = parameters[index].ParameterType;
            }

            MethodAttributes attributes = MethodAttributes.Public;

            if (constructor.DeclaringType != typeof(object)
                && !constructor.DeclaringType.IsAbstract)
            {
                attributes = constructor.Attributes;
            }

            ConstructorBuilder cBuilder =
                typeBuilder.DefineConstructor(attributes, constructor.CallingConvention, parameterTypes);

            ILGenerator ilGenerator = cBuilder.GetILGenerator();

            // initialize interceptor first.
            ilGenerator.Emit(OpCodes.Ldarg_0);
            // store it in a field.
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, interceptors);

            if (!mockedConstructor)
            {
                if (constructor.IsFamily | constructor.IsPublic | constructor.IsAssembly)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);

                    for (int index = 1; index < parameterTypes.Length; index++)
                    {
                        ilGenerator.Emit(OpCodes.Ldarg, index + 1);
                    }

                    ilGenerator.Emit(OpCodes.Call, constructor);
                }
            }

            ilGenerator.Emit(OpCodes.Ret);
        }

        private void RegisterIntercptors(Type target)
        {
            RegisterInterceptor(target);

            // Register interceptor to all the dependent interfaces as well as for methods of the current proxy.
            RegisterInterceptorForInterfaces(target);
        }

        private void RegisterInterceptorForInterfaces(Type targetType)
        {
            foreach (Type @interface in targetType.GetInterfaces())
            {
                if ((targetType.BaseType != typeof(object) && !IsLocalToType(@interface, targetType)))
                {
                    RegisterInterceptor(@interface);
                }
            }
        }

        private bool IsLocalToType(Type @interface, Type targetType)
        {
            return targetType.GetMethods().Any((MethodInfo methodInfo) =>
            {
                return @interface.GetMethods().Any(x => Filter(x, methodInfo));
            });
        }

        private void RegisterInterceptor(Type target)
        {
            EmitMethods(target, BindingFlags.Public | BindingFlags.Instance);
            EmitMethods(target, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void EmitMethods(Type target, BindingFlags flags)
        {
            foreach (var methodInfo in target.GetMethods(flags))
            {
                if (IsValid(methodInfo))
                {
                    new MethodEmitter(this).Emit(methodInfo);
                }
            }
        }

        private bool IsValid(MethodInfo method)
        {
            bool isValid = false;

            if (method.IsVirtual && !method.IsFinal)
            {
                if (method.IsPublic || IsFamilyOrAssembly(method))
                {
                    bool objectOverrides = method.IsObjectOverrides();
                    // override object methods if necessary
                    isValid = incldueObjectOverrides && objectOverrides;

                    if (method.DeclaringType != typeof(object))
                    {
                        isValid = method.IsPublic || IsFamilyOrAssembly(method);

                        // check if similar final method hiding the virtual. 
                        if (!objectOverrides)
                        {
                            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

                            var targetMethod = this.target.GetMethods(flags).Where(x => Filter(x, method)).FirstOrDefault();

                            isValid &= !(targetMethod != null && targetMethod.IsFinal);
                        }
                    }
                }
            }

            return isValid;
        }

        private bool Filter(MethodInfo source, MethodInfo target)
        {
            if (source.Name == target.Name)
            {
                var methodParameters = source.GetParameters();
                var targetParameters = target.GetParameters();

                if (source.IsGenericMethod != target.IsGenericMethod)
                    return false;

                if (targetParameters.Length != methodParameters.Length)
                    return false;

                int index = 0;
                bool result = true;

                foreach (var parameter in targetParameters)
                {
                    if (parameter.ParameterType != methodParameters[index].ParameterType)
                    {
                        result = false;
                        break;
                    }
                    index++;
                }
                return result;
            }

            return false;
        }

        private bool IsFamilyOrAssembly(MethodInfo methodInfo)
        {
            bool result = methodInfo.IsAssembly && IsInternalsVisibleTo(methodInfo.Module);

            return result || methodInfo.IsFamily | methodInfo.IsFamilyOrAssembly;
        }

        private bool IsInternalsVisibleTo(Module module)
        {
            bool result = false;

            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            object[] args = module.Assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false);

            if (args.Length > 0)
            {
                foreach (var att in args)
                {
                    string assName = ((InternalsVisibleToAttribute)att).AssemblyName;

                    if (assName.IndexOf(',') > 0)
                    {
                        assName = assName.Substring(0, assName.IndexOf(','));
                    }

                    if (executingAssembly.FullName.IndexOf(assName) >= 0)
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///  Gets the interceptor associated with this type.
        /// </summary>
        public FieldInfo Interceptors
        {
            get
            {
                return interceptors;
            }
        }

        /// <summary>
        /// Gets the typebuilder associatedw the emitter.
        /// </summary>
        public TypeBuilder Builder
        {
            get
            {
                return typeBuilder;
            }
        }

        public FieldInfo InterceptorIndex
        {
            get
            {
                return interceptorIndex;
            }
        }


        /// <summary>
        /// Creates the underlying type.
        /// </summary>
        /// <returns></returns>
        public Type CreateType()
        {
            return typeBuilder.CreateType();
        }

        private Type target;
        private FieldBuilder interceptors;
        private TypeBuilder typeBuilder;
        private readonly ModuleBuilder module;
        private FieldInfo interceptorIndex;
        private bool incldueObjectOverrides;
        public bool mockedConstructor;
    }
}
