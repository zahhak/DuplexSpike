using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
	public abstract class ProxyBase<T> : IDisposable
	{
		private static readonly JsonSerializer jsonSerializer;
		private static readonly ConcurrentDictionary<string, ActionExecutor> executors;

		private readonly Func<object, Task> sendFunc;
		private readonly Action disposalAction;
		private readonly ConcurrentDictionary<long, dynamic> pendingOperations;
		private readonly T targetObject;

		private long lastOperationId;

		static ProxyBase()
		{
			jsonSerializer = new JsonSerializer();
			executors = new ConcurrentDictionary<string, ActionExecutor>();
		}

		public ProxyBase(Func<object, Task> sendFunc, T targetObject)
			:this(sendFunc, targetObject, null)
		{
		}

		public ProxyBase(Func<object, Task> sendFunc, T targetObject, Action disposalAction)
		{
			this.disposalAction = disposalAction;
			this.targetObject = targetObject;
			this.pendingOperations = new ConcurrentDictionary<long, dynamic>();
			this.sendFunc = sendFunc;
		}

		public async Task OnReceivedAsync(dynamic incomingData)
		{
			var incomingDataDictionary = (IDictionary<string, JToken>)incomingData;
			if (incomingDataDictionary.ContainsKey("MethodName"))
			{
				string methodName = incomingData.MethodName;
				var methodInfo = typeof(T).GetMethod(methodName);
				var executor = executors.GetOrAdd(methodName, _ => new ActionExecutor(methodInfo));
				JObject parameters = incomingData.Parameters;
				var arguments = executor.MethodInfo.ParseArguments(parameters, jsonSerializer);
				object response = null;
				try
				{
					var result = await executor.Execute(targetObject, arguments);
					response = new { OperationId = (long)incomingData.OperationId, Result = result };
				}
				catch (Exception ex)
				{
					response = new { OperationId = (long)incomingData.OperationId, Exception = ex };
				}
				await this.sendFunc(response);
			}
			else if (incomingDataDictionary.ContainsKey("OperationId"))
			{
				dynamic taskCompletionSource;
				long operationId = incomingData.OperationId;
				if (this.pendingOperations.TryRemove(operationId, out taskCompletionSource))
				{
					SetResult(taskCompletionSource, incomingData);
				}
			}
		}
  
		private static void SetResult(dynamic taskCompletionSource, dynamic value)
		{
			if (value.Result == null)
			{
				taskCompletionSource.SetResult(null);
			}
			else
			{
				Type innerType = taskCompletionSource.GetType().GetGenericArguments()[0];
				var deserializedResult = value.Result.ToObject(innerType, jsonSerializer);
				taskCompletionSource.SetResult(deserializedResult);
			}
		}

		public void Dispose()
		{
			if (this.disposalAction != null)
			{
				this.disposalAction();
			}
		}

		public void SendAsync(IOperationDescriptor operationDescriptor, dynamic taskCompletionSource)
		{
			var id = Interlocked.Increment(ref this.lastOperationId);
			this.pendingOperations.TryAdd(id, taskCompletionSource);
			operationDescriptor.OperationId = id;
			sendFunc(operationDescriptor);
		}
	}
}
