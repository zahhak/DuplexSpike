using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telerik.DynamicProxy;

namespace LongRunningSignalR
{
	public class ServiceConnection<TService, TSession, TCallback> : PersistentConnection
		where TSession : IOperationSession<TCallback>
	{
		private static readonly Lazy<JsonSerializer> applicationJsonSerializer;
		private static readonly Lazy<JsonSerializer> iceniumJsonSerializer;
		private readonly ConcurrentDictionary<string, ActionExecutor> executors;

		private IConnectionContext<TService, TSession, TCallback> connectionContext;

		private TService service;

		static ServiceConnection()
		{
			applicationJsonSerializer = new Lazy<JsonSerializer>();
			iceniumJsonSerializer = new Lazy<JsonSerializer>(() => new JsonSerializer { TypeNameHandling = TypeNameHandling.All });
		}

		public ServiceConnection()
		{
			this.executors = new ConcurrentDictionary<string, ActionExecutor>();
		}

		public override void Initialize(IDependencyResolver resolver, HostContext context)
		{
 			base.Initialize(resolver, context);
			this.service = resolver.Resolve<TService>();
			this.connectionContext = resolver.Resolve<IConnectionContext<TService, TSession, TCallback>>();
		}

		protected async override Task OnReceivedAsync(IRequest request, string connectionId, string data)
		{
			var serializer = SelectJsonSerializer(request);

			dynamic incomingData = JsonConvert.DeserializeObject<dynamic>(data);
			var incomingDataDictionary = (IDictionary<string, JToken>)incomingData;
			if (incomingDataDictionary.ContainsKey("MethodName"))
			{
				await this.ProcessOperationAsync(connectionId, incomingData, serializer);
			}
			else if (incomingDataDictionary.ContainsKey("OperationId"))
			{
				//DUPLICATE CODE
				object taskCompletionSource;
				if (this.connectionContext.PendingRequests.TryRemove(incomingData.OperationId.Value, out taskCompletionSource))
				{
					var taskCompletionSourceGenericType = taskCompletionSource.GetType().GetGenericArguments()[0];
					if (incomingDataDictionary.ContainsKey("Result"))
					{
						dynamic result = incomingDataDictionary["Result"].ToObject(taskCompletionSourceGenericType, iceniumJsonSerializer.Value);
						((dynamic)taskCompletionSource).SetResult(result);
					}
					else if (incomingDataDictionary.ContainsKey("Exception"))
					{
						var exceptionType = Type.GetType(incomingData.Exception.ClassName.Value);
						dynamic exception = incomingDataDictionary["Exception"].ToObject(exceptionType, iceniumJsonSerializer.Value);
						((dynamic)taskCompletionSource).SetException(exception);
					}
				}
			}
		}

		private async Task ProcessOperationAsync(string connectionId, dynamic operationDescriptor, JsonSerializer serializer)
		{
			var didCreateSession = false;

			var session = this.GetOrCreateSession(connectionId, operationDescriptor, serializer, out didCreateSession);
			dynamic response = new { OperationId = operationDescriptor.OperationId };

			if (didCreateSession)
			{
				var callback = this.CreateCallback(connectionId, serializer);
				session.Initialize(callback);
			}
			else
			{
				var methodName = (string)operationDescriptor.MethodName;
				var executor = this.executors.GetOrAdd(methodName, CreateExecutor);
				var arguments = ParseArguments(operationDescriptor.Parameters, serializer, executor.MethodInfo);
				
				try
				{
					var result = await executor.Execute(session, arguments);
					response = new { OperationId = operationDescriptor.OperationId, Result = result};
				}
				catch (Exception ex)
				{
					response = new { OperationId = operationDescriptor.OperationId, Exception = ex };
				}
			}

			await this.Connection.Send(connectionId, serializer.Serialize((object)response));
		}

		private TCallback CreateCallback(string connectionId, JsonSerializer serializer)
		{
			var factory = new ProxyFactory(typeof(TCallback));
			factory.Register(new CallbackMethodInterceptor(
				o => this.SendAsync(connectionId, o),
				(o, type) =>
				{
					var m = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(method => method.Name == "SendAsync" && method.IsGenericMethod);
					var generic = m.MakeGenericMethod(type);
					return	generic.Invoke(this, new object[] { connectionId, o });
				},
				serializer));
			factory.CallingConstructor = new Ctor();
			return (TCallback)(object)factory.Create();
		}

		internal Task<TResult> SendAsync<TResult>(string connectionId, dynamic payload)
		{
			var tcs = new TaskCompletionSource<TResult>();
			this.connectionContext.PendingRequests.TryAdd(payload.OperationId, tcs);
			ConnectionExtensions.Send(this.Connection, connectionId, payload);
			return tcs.Task;
		}

		internal Task SendAsync(string connectionId, dynamic payload)
		{
			var tcs = new TaskCompletionSource<object>();
			this.connectionContext.PendingRequests.TryAdd(payload.OperationId, tcs);
			ConnectionExtensions.Send(this.Connection, connectionId, payload);
			return tcs.Task;
		}

		private TSession GetOrCreateSession(string connectionId, dynamic operationDescriptor, JsonSerializer serializer, out bool didCreateSession)
		{
			var methodName = operationDescriptor.MethodName.Value;
			var contractType = this.service.GetType();
			MethodInfo methodInfo = contractType.GetMethod(methodName);

			if (methodInfo == null)
			{
				throw new Exception(string.Format("Missing operation: {0}, {1}", contractType.Name, methodName));
			}

			var parsedParameters = ParseArguments(operationDescriptor.Parameters, serializer, methodInfo);
			var session = this.connectionContext.GetOrCreateSession(connectionId, this.service, methodInfo, parsedParameters, out didCreateSession);

			return session;
		}

		private static IDictionary<string, object> ParseArguments(JObject arguments, JsonSerializer serializer, MethodInfo targetMethod)
		{
			var parameters = (IDictionary<string, JToken>)arguments;
			return targetMethod.GetParameters().Select(param =>
			{
				return new { ParameterName = param.Name, Value = parameters[param.Name].ToObject(param.ParameterType, serializer) };
			}).ToDictionary(kvp => kvp.ParameterName, kvp => kvp.Value);
		}

		private static JsonSerializer SelectJsonSerializer(IRequest request)
		{
			var userAgent = request.Headers.GetValues("User-Agent").FirstOrDefault() ?? string.Empty;
			if (userAgent.IndexOf("Graphite", StringComparison.Ordinal) > -1)
			{
				return iceniumJsonSerializer.Value;
			}

			return applicationJsonSerializer.Value;
		}

		private static ActionExecutor CreateExecutor(string methodName)
		{
			var methodInfo = typeof(TSession).GetMethod(methodName);
			return new ActionExecutor(methodInfo);
		}
	}
}