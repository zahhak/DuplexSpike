using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Common
{
	public sealed class ActionExecutor
	{
		private readonly Func<object, object[], Task<object>> _executor;
		private readonly Lazy<ParameterInfo[]> methodParameters;
		private static MethodInfo _convertOfTMethod = typeof(ActionExecutor).GetMethod("Convert", BindingFlags.Static | BindingFlags.NonPublic);

		public MethodInfo MethodInfo { get; private set; }

		public ActionExecutor(MethodInfo methodInfo)
		{
			Contract.Assert(methodInfo != null);
			this._executor = GetExecutor(methodInfo);
			this.methodParameters = new Lazy<ParameterInfo[]>(() => methodInfo.GetParameters());

			this.MethodInfo = methodInfo;
		}

		public Task<object> Execute(object instance, object[] arguments)
		{
			return _executor(instance, arguments);
		}

		public Task<object> Execute(object instance, IDictionary<string, object> arguments)
		{
			var parameters = this.methodParameters.Value;
			if (parameters.Length == 0)
			{
				return this.Execute(instance, new object[0]);
			}

			var parameterValues = new object[parameters.Length];
			for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
			{
				object value;
				arguments.TryGetValue(parameters[parameterIndex].Name, out value);
				parameterValues[parameterIndex] = value;
			}

			return this.Execute(instance, parameterValues);
		}

		// Method called via reflection.
		private static Task<object> Convert<T>(object taskAsObject)
		{
			Task<T> task = (Task<T>)taskAsObject;
			return task.CastToObject<T>();
		}

		// Do not inline or optimize this method to avoid stack-related reflection demand issues when
		// running from the GAC in medium trust
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private static Func<object, Task<object>> CompileGenericTaskConversionDelegate(Type taskValueType)
		{
			Contract.Assert(taskValueType != null);

			return (Func<object, Task<object>>)Delegate.CreateDelegate(typeof(Func<object, Task<object>>), _convertOfTMethod.MakeGenericMethod(taskValueType));
		}

		private static Func<object, object[], Task<object>> GetExecutor(MethodInfo methodInfo)
		{
			// Parameters to executor
			ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
			ParameterExpression parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

			// Build parameter list
			List<Expression> parameters = new List<Expression>();
			ParameterInfo[] paramInfos = methodInfo.GetParameters();
			for (int i = 0; i < paramInfos.Length; i++)
			{
				ParameterInfo paramInfo = paramInfos[i];
				BinaryExpression valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
				UnaryExpression valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

				// valueCast is "(Ti) parameters[i]"
				parameters.Add(valueCast);
			}

			// Call method
			UnaryExpression instanceCast = (!methodInfo.IsStatic) ? Expression.Convert(instanceParameter, methodInfo.ReflectedType) : null;
			MethodCallExpression methodCall = methodCall = Expression.Call(instanceCast, methodInfo, parameters);

			// methodCall is "((MethodInstanceType) instance).method((T0) parameters[0], (T1) parameters[1], ...)"
			// Create function
			if (methodCall.Type == typeof(void))
			{
				// for: public void Action()
				Expression<Action<object, object[]>> lambda = Expression.Lambda<Action<object, object[]>>(methodCall, instanceParameter, parametersParameter);
				Action<object, object[]> voidExecutor = lambda.Compile();
				return (instance, methodParameters) =>
				{
					voidExecutor(instance, methodParameters);
					return TaskHelpers.NullResult();
				};
			}
			else
			{
				// must coerce methodCall to match Func<object, object[], object> signature
				UnaryExpression castMethodCall = Expression.Convert(methodCall, typeof(object));
				Expression<Func<object, object[], object>> lambda = Expression.Lambda<Func<object, object[], object>>(castMethodCall, instanceParameter, parametersParameter);
				Func<object, object[], object> compiled = lambda.Compile();
				if (methodCall.Type == typeof(Task))
				{
					// for: public Task Action()
					return (instance, methodParameters) =>
					{
						Task r = (Task)compiled(instance, methodParameters);
						ThrowIfWrappedTaskInstance(methodInfo, r.GetType());
						return r.CastToObject();
					};
				}
				else if (typeof(Task).IsAssignableFrom(methodCall.Type))
				{
					// for: public Task<T> Action()
					// constructs: return (Task<object>)Convert<T>(((Task<T>)instance).method((T0) param[0], ...))
					Type taskValueType = GetTaskInnerTypeOrNull(methodCall.Type);
					var compiledConversion = CompileGenericTaskConversionDelegate(taskValueType);

					return (instance, methodParameters) =>
					{
						object callResult = compiled(instance, methodParameters);
						Task<object> convertedResult = compiledConversion(callResult);
						return convertedResult;
					};
				}
				else
				{
					// for: public T Action()
					return (instance, methodParameters) =>
					{
						var result = compiled(instance, methodParameters);
						// Throw when the result of a method is Task. Asynchronous methods need to declare that they
						// return a Task.
						Task resultAsTask = result as Task;
						if (resultAsTask != null)
						{
							throw new InvalidOperationException("ActionExecutor_UnexpectedTaskInstance");
						}
						return TaskHelpers.FromResult(result);
					};
				}
			}
		}

		private static void ThrowIfWrappedTaskInstance(MethodInfo method, Type type)
		{
			// Throw if a method declares a return type of Task and returns an instance of Task<Task> or Task<Task<T>>
			// This most likely indicates that the developer forgot to call Unwrap() somewhere.
			Contract.Assert(method.ReturnType == typeof(Task));
			// Fast path: check if type is exactly Task first.
			if (type != typeof(Task))
			{
				Type innerTaskType = GetTaskInnerTypeOrNull(type);
				if (innerTaskType != null && typeof(Task).IsAssignableFrom(innerTaskType))
				{
					throw new InvalidOperationException("ActionExecutor_WrappedTaskInstance");
				}
			}
		}

		private static Type GetTaskInnerTypeOrNull(Type type)
		{
			Contract.Assert(type != null);
			if (type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				Type genericTypeDefinition = type.GetGenericTypeDefinition();
				// REVIEW: should we consider subclasses of Task<> ??
				if (typeof(Task<>) == genericTypeDefinition)
				{
					return type.GetGenericArguments()[0];
				}
			}

			return null;
		}
	}
}