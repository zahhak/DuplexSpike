using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Telerik.DynamicProxy.Enhancers
{
    ///<summary>
    /// Contains helper methods for various repeatative tasks.
    ///</summary>
    internal static class BasicEnhancer
    {
        internal static bool IsAssignable(this Type source, Type target)
        {
            if (!source.IsGenericType)
                return false;
            return source.GetGenericTypeDefinition().IsAssignableFrom(target);
        }

        internal static object CreateEmptyInstanceFrom(this Type source, Type target)
        {
            var genericType = source.MakeGenericType(target.GetGenericArguments());
            return Activator.CreateInstance(genericType);
        }
        
        /// <summary>
        /// Gets the original type omitting any out/ref initials.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static Type GetRealType(this Type parameter)
        {
            Type target = parameter;
            bool isRef = parameter.IsByRef;

            if (isRef)
            {
                target = parameter.GetElementType();
            }
            return target;
        }

        public static object[] ExtendWithInterceptor(object[] args, object[] interceptors)
        {
            var extendedArgs = new object[args.Length + 1];

            extendedArgs[0] = interceptors;
            args.CopyTo(extendedArgs, 1);

            return extendedArgs;
        }

        ///<summary>
        /// Extracts the correct type for defined generic types.
        ///</summary>
        ///<param name="paramType"></param>
        ///<param name="genericTypeMaps"></param>
        ///<returns></returns>
        public static Type ExtractCorrectType(this Type paramType, IDictionary<string, GenericTypeParameterBuilder> genericTypeMaps)
        {
            GenericTypeParameterBuilder builder2;
            if (paramType.IsArray)
            {
                int arrayRank = paramType.GetArrayRank();
                Type elementType = paramType.GetElementType();
                if (elementType.IsGenericParameter)
                {
                    GenericTypeParameterBuilder builder;
                    if (!genericTypeMaps.TryGetValue(elementType.Name, out builder))
                    {
                        return paramType;
                    }
                    if (arrayRank == 1)
                    {
                        return builder.MakeArrayType();
                    }
                    return builder.MakeArrayType(arrayRank);
                }
                if (arrayRank == 1)
                {
                    return elementType.MakeArrayType();
                }
                return elementType.MakeArrayType(arrayRank);
            }
            if (paramType.IsGenericParameter && genericTypeMaps.TryGetValue(paramType.Name, out builder2))
            {
                return builder2;
            }
            return paramType;
        }

        internal static void ProcessGenericArguments(this GenericTypeParameterBuilder[] builders, Type[] genericArguments)
        {
            for (int i = 0; i < genericArguments.Length; ++i)
            {
                builders[i].SetGenericParameterAttributes(genericArguments[i].GenericParameterAttributes);

                List<Type> ifcConstraints = null;
                foreach (Type type in genericArguments[i].GetGenericParameterConstraints())
                {
                    Type runtimeType = Type.GetTypeFromHandle(type.TypeHandle);

                    if (type.IsClass)
                    {
                        builders[i].SetBaseTypeConstraint(runtimeType);
                    }
                    else
                    {
                        if (ifcConstraints == null)
                        {
                            ifcConstraints = new List<Type>();
                        }
                        ifcConstraints.Add(runtimeType);
                    }
                }
                if (ifcConstraints != null)
                {
                    builders[i].SetInterfaceConstraints(ifcConstraints.ToArray());
                }
            }
        }

    }
}
