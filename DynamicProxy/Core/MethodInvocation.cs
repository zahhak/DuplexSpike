using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Telerik.DynamicProxy.Abstraction;
using System.Diagnostics;

namespace Telerik.DynamicProxy
{
    /// <summary>
    /// MethodInovcation entry-point class.
    /// </summary>
    public class MethodInvocation : IInvocation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodInvocation"></see> class.
        /// </summary>
        /// <param name="target">Target instance</param>
        /// <param name="method">Target method</param>
        /// <param name="args">Invocation arguments</param>
        public MethodInvocation(object target, MethodInfo method, Argument[] args)
        {
            this.target = target;
            this.method = method;
            this.args = args;

            // name of the invocation.
            name = method.Name;
        }

        /// <summary>
        /// Gets the name of the invocation.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Gets or set the current interceptor index
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets the declaring type.
        /// </summary>
        public Type DeclaringType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the target instance
        /// </summary>
        public object Target
        {
            get { return target; }
        }

        /// <summary>
        /// Gets the arguments for current invocation.
        /// </summary>
        public Argument[] Arguments
        {
            get
            {
                return args;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the proxy
        /// should call the base method.
        /// </summary>
        public bool InvokeBase
        {
            get;
            set;
        }

        /// <summary>
        /// Sets the return value from interceptor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void SetReturnInternal<T>(T value)
        {
            SetReturn(value);
        }

        /// <summary>
        /// Sets the return value for the interception.
        /// </summary>
        /// <param name="value"></param>
        public void SetReturn(object value)
        {
            if (this.method.ReturnType.IsValueType || this.method.ReturnType.IsArray)
            {
                returnValue = this.method.ReturnType.GetDefaultValue();
            }
            if (value != null)
            {
                returnValue = value;
            }
        }

        /// <summary>
        /// Gets the name of callee
        /// </summary>
        public MethodInfo Method
        {
            get
            {
                return method;
            }
        }


        /// <summary>
        /// Gets the user set return value for the current invocation.
        /// </summary>
        public object ReturnValue
        {
            get
            {
                return this.returnValue;
            }
        }

        /// <summary>
        /// Continues the execution flow to the next interceptor (if any), or  to the 
        /// main method and cascade the changes to the main interceptor.
        /// </summary>
        public void Continue()
        {
            try
            {
                MethodInfo mi = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
                IList<Type> types = ExpandArguments(mi);

                MethodInfo interceptorMethod = FindExtendedMethod(mi, types);

                if (interceptorMethod.IsGenericMethod && interceptorMethod.IsGenericMethodDefinition)
                {
                    interceptorMethod = interceptorMethod.MakeGenericMethod(method.GetGenericArguments());
                }

                IList<object> values = args.Select(arg => arg.Value).ToList();

                values.Add(this);
                values.Add(++Index);
                values.Add(true);

                object[] valuesArray = values.ToArray();
                object obj = interceptorMethod.Invoke(target, valuesArray);

                SetOutArgs(valuesArray);
                SetReturn(obj);
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Expands the specified methods arguments
        /// </summary>
        /// <param name="mi">Target method</param>
        /// <returns>Expanded list of arguments</returns>
        private static IList<Type> ExpandArguments(MethodInfo mi)
        {
            IList<Type> types = mi.GetParameters().Select(arg => arg.ParameterType).ToList();

            types.Add(typeof(MethodInvocation));
            types.Add(typeof(int));
            types.Add(typeof(bool));

            return types;
        }

        /// <summary>
        /// Tries find the specified method.
        /// </summary>
        /// <param name="methodInfo">Similar method to find.</param>
        /// <param name="types">List of parameter types.</param>
        /// <returns>Target method</returns>
        private MethodInfo FindExtendedMethod(MethodInfo methodInfo, IList<Type> types)
        {
            var proxyType = target.GetType();

            Func<MethodInfo, bool> predicate = x => x.Name == "_" + methodInfo.Name;

            var proxyMethods = proxyType.GetMethods().Where(predicate);

            foreach (var proxyMethod in proxyMethods)
            {
                var valid = true;
                var parameters = proxyMethod.GetParameters();

                valid &= parameters.Length == types.Count;

                if (valid)
                    for (int index = 0; index < parameters.Length; index++)
                    {
                        valid &= parameters[index].ParameterType.Name == types[index].Name;
                    }

                valid &= proxyMethod.GetGenericArguments().Length == methodInfo.GetGenericArguments().Length;

                if (valid)
                {
                    return proxyMethod;
                }
            }
            return null;
        }

        private void SetOutArgs(object[] valuesArray)
        {
            for (int index = 0; index < args.Length; index++)
            {
                if (args[index].IsOut)
                {
                    args[index].Value = valuesArray[index];
                }
            }
        }

        private MethodInfo method;
        private Argument[] args;
        private object target;
        private object returnValue;
        private string name;
    }
}
