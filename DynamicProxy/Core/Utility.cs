using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace Telerik.DynamicProxy
{
    internal static class Utility
    {
        ///<summary>
        /// Converts the <see cref="ParameterInfo"/> array to its type array form.
        ///</summary>
        ///<param name="parameterInfos"></param>
        ///<returns></returns>
        public static Type[] ToTypeArray(ParameterInfo[] parameterInfos)
        {
            Type[] parameterTypes = new Type[parameterInfos.Length];

            int index = 0;

            foreach (ParameterInfo parameterInfo in parameterInfos)
            {
                parameterTypes[index] = parameterInfo.ParameterType;
                index++;
            }
            return parameterTypes;
        }

        internal static MethodAttributes GetTargetAttributes(MethodInfo info)
        {
            MethodAttributes methodAttributes = MethodAttributes.Virtual;

            if (info.IsFamily)
                methodAttributes |= MethodAttributes.Family;

            if (info.IsFamilyAndAssembly)
                methodAttributes |= MethodAttributes.FamANDAssem;

            if (info.IsFamilyOrAssembly)
                methodAttributes |= MethodAttributes.FamORAssem;

            if (info.IsAssembly)
                methodAttributes |= MethodAttributes.Assembly;

            if (info.IsHideBySig)
                methodAttributes |= MethodAttributes.HideBySig;

            if (info.IsPublic)
                methodAttributes |= MethodAttributes.Public;

            return methodAttributes;
        }

        internal static bool IsObjectOverrides(this MethodInfo method)
        {
            string[] overrideMethodNames = { "ToString", "GetHashCode", "Equals" };
            foreach (var overrideMethodName in overrideMethodNames)
            {
                if (string.Compare(overrideMethodName, method.Name) == 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the default value for target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        internal static object GetDefaultValue(this Type target)
        {
            if (target != typeof(void))
            {
                if (target.IsValueType)
                    return Activator.CreateInstance(target);
                if (target.IsArray)
                    return Activator.CreateInstance(target, 0);
            }
            return null;
        }
    
    }
}
