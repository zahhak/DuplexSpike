using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telerik.DynamicProxy;
using Telerik.DynamicProxy.Abstraction;

namespace SignalRClient
{
	partial class DuplexOperationExecutor<TOperation, TCallback> : HttpOperationExecutor<TOperation>
	{
		private const string CreateSessionOperationId = "CreateSessionOperationId";
		private static readonly ConcurrentDictionary<string, ActionExecutor> executors;
		private readonly ConcurrentDictionary<string, object> operations;
		private static Lazy<JsonSerializer> iceniumJsonSerializer;
		private Connection connection;
		private TCallback Callback;

		static DuplexOperationExecutor()
		{
			executors = new ConcurrentDictionary<string, ActionExecutor>();
			iceniumJsonSerializer = new Lazy<JsonSerializer>(() => new JsonSerializer { TypeNameHandling = TypeNameHandling.All });
		}

		public DuplexOperationExecutor(HttpOperationContext context, TCallback callback) : base(context)
		{
			this.Callback = callback;
			this.operations = new ConcurrentDictionary<string, object>();
		}

		public async Task Initialize()
		{
			this.connection = new Connection("http://iiliev:81/duplex/build");
			
			this.connection.Received += this.OnServerConnectionReceived;

			this.connection.Error += data => Console.WriteLine(data);

			await connection.Start();
		}

		private async void OnServerConnectionReceived(string data)
		{
			dynamic incomingData = JsonConvert.DeserializeObject<dynamic>(data);
			var incomingDataDictionary = (IDictionary<string, JToken>)incomingData;
			if (incomingDataDictionary.ContainsKey("MethodName"))
			{
				//DUPLICATE CODE
				var methodName = incomingData.MethodName.Value;
				var contractType = this.Callback.GetType();
				MethodInfo methodInfo = contractType.GetMethod(methodName);

				if (methodInfo == null)
				{
					throw new Exception(string.Format("Missing operation: {0}, {1}", contractType.Name, methodName));
				}

				var executor = executors.GetOrAdd(methodName, new ActionExecutor(methodInfo));
				var arguments = ParseArguments(incomingData.Parameters, iceniumJsonSerializer.Value, methodInfo);

				try
				{
					var result = await executor.Execute(this.Callback, arguments);
					this.connection.Send(new { OperationId = incomingData.OperationId, Result = result });
				}
				catch (Exception ex)
				{
					this.connection.Send(new { OperationId = incomingData.OperationId, Exception = ex });
				}
			}
			else if (incomingDataDictionary.ContainsKey("OperationId"))
			{
				object taskCompletionSource;
				if (operations.TryGetValue(incomingData.OperationId.Value, out taskCompletionSource) & taskCompletionSource.GetType().IsGenericType)
				{
					var taskCompletionSourceGenericType = taskCompletionSource.GetType().GetGenericArguments()[0];
					var isSession = typeof(IOperationSession).IsAssignableFrom(taskCompletionSourceGenericType);
					if (isSession)
					{
						var factory = new ProxyFactory(taskCompletionSourceGenericType);
						factory.Register(new SessionProxyMethodInterceptor<TOperation, TCallback>(this));
						factory.CallingConstructor = new Ctor();
						dynamic proxy = factory.Create();
						((dynamic)taskCompletionSource).SetResult(proxy);
					}
					else
					{
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
		}

		//DUPLICATE CODE
		private static IDictionary<string, object> ParseArguments(JObject arguments, JsonSerializer serializer, MethodInfo targetMethod)
		{
			var parameters = (IDictionary<string, JToken>)arguments;
			return targetMethod.GetParameters().Select(param =>
			{
				return new { ParameterName = param.Name, Value = parameters[param.Name].ToObject(param.ParameterType, serializer) };
			}).ToDictionary(kvp => kvp.ParameterName, kvp => kvp.Value);
		}

		protected internal override Task Enqueue(OperationDescriptor descriptor)
		{
			var json = SerializeDescriptor(descriptor);
			var tcs = new TaskCompletionSource<object>();
			this.connection.Send(json);
			this.operations.AddOrUpdate(descriptor.OperationId, tcs, (oldTcs, newTcs) => tcs);
			return tcs.Task;
		}

		protected internal override Task<TResult> Enqueue<TResult>(OperationDescriptor descriptor)
		{
			var json = SerializeDescriptor(descriptor);
			var tcs = new TaskCompletionSource<TResult>();
			this.connection.Send(json);
			this.operations.AddOrUpdate(descriptor.OperationId, tcs, (oldTcs, newTcs) => tcs);
			return tcs.Task;
		}

		private string SerializeDescriptor(OperationDescriptor descriptor)
		{
			return JValue.FromObject(descriptor, iceniumJsonSerializer.Value).ToString();
		}
	}
}
