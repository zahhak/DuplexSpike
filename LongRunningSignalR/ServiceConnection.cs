using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LongRunningSignalR
{
	public class ServiceConnection<TService, TSession, TCallback> : PersistentConnection
		where TSession : IOperationSession<TCallback>
	{
		private readonly static JsonSerializer jsonSerializer;

		private ConnectionContext<TService, TSession, TCallback> connectionContext;

		private TService service;


		static ServiceConnection()
		{
			jsonSerializer = new JsonSerializer();
		}

		public override void Initialize(IDependencyResolver resolver, HostContext context)
		{
 			base.Initialize(resolver, context);
			this.service = resolver.Resolve<TService>();
			this.connectionContext = resolver.Resolve<ConnectionContext<TService, TSession, TCallback>>();
		}

		protected async override Task OnReceivedAsync(IRequest request, string connectionId, string data)
		{
			dynamic incomingData = JsonConvert.DeserializeObject<dynamic>(data);

			TCallback callback;
			if (this.connectionContext.TryGetCallback(connectionId, out callback))
			{
				await ((dynamic)callback).OnReceivedAsync(incomingData);
			}
			else
			{
				var didCreateSession = false;
				var session = await this.GetOrCreateSession(connectionId, incomingData, out didCreateSession);
				callback = CreateCallback(connectionId, this.Connection, session);
				this.connectionContext.AddCallback(connectionId, callback);
				session.Initialize(callback);

				object response = new { OperationId = (long)incomingData.OperationId };
				await this.Connection.Send(connectionId, jsonSerializer.Serialize(response));
			}
		}

		private static TCallback CreateCallback(string connectionId, IConnection connection, TSession session)
		{
			Func<object, Task> sendFunc = payload => 
				{
					var serializedPayload = jsonSerializer.Serialize(payload);
					return ConnectionExtensions.Send(connection, connectionId, serializedPayload);
				};

			return DynamicProxyFactory.CreateProxy<TCallback, TSession>(sendFunc, session);
		}

		private Task<TSession> GetOrCreateSession(string connectionId, dynamic operationDescriptor, out bool didCreateSession)
		{
			string methodName = operationDescriptor.MethodName;
			var methodInfo = typeof(TService).GetMethod(methodName);
			JObject parameters = operationDescriptor.Parameters;
			var parsedParameters = methodInfo.ParseArguments(parameters, jsonSerializer);

			return this.connectionContext.GetOrCreateSessionAsync(connectionId, this.service, methodInfo, parsedParameters, out didCreateSession);
		}

		protected override Task OnDisconnectAsync(IRequest request, string connectionId)
		{
			this.connectionContext.DisposeSession(connectionId);
			return base.OnDisconnectAsync(request, connectionId);
		}
	}
}