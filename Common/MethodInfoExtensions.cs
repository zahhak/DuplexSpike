using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
	public static class MethodInfoExtensions
	{
		public static IDictionary<string, object> ParseArguments(this MethodInfo method, JObject arguments, JsonSerializer serializer)
		{
			var parameters = (IDictionary<string, JToken>)arguments;
			return method.GetParameters().Select(param =>
			{
				return new { ParameterName = param.Name, Value = parameters[param.Name].ToObject(param.ParameterType, serializer) };
			}).ToDictionary(kvp => kvp.ParameterName, kvp => kvp.Value);
		}
	}
}
