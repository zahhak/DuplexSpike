using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LongRunningSignalR
{
	public static class TypeExtensions
	{
		public static MethodInfo GetMethod(this Type type, string methodName, IEnumerable<string> parameterNames)
		{
			var methodInfos = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			var method = methodInfos.Where(methodInfo =>
												methodInfo.Name == methodName &&
												methodInfo.GetParameters().Select(parameter => parameter.Name).SequenceEqual(parameterNames))
									.Single();
			return method;
		}
	}
}