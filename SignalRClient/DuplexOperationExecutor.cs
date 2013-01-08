using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telerik.DynamicProxy;

namespace SignalRClient
{
	public class DuplexOperationExecutor<TServiceContract>
	{
		public async Task<TSession> Execute<TSession, TCallback>(Func<TServiceContract, Task<TSession>> setup, TCallback callback)
			where TSession : IOperationSession<TCallback>
		{
			//Build Url
			var connection = new Connection("http://iiliev:81/duplex/build");
			
			await connection.Start();

			await this.NegotiateSession(setup, connection);

			Func<object, Task> sendFunc = connection.Send;

			Action disconnectAction = () =>
				{
					connection.Stop();
					connection.Disconnect();
				};
			var proxy =  DynamicProxyFactory.CreateProxy<TSession, TCallback>(sendFunc, callback, disconnectAction);
			connection.Received += data => ((dynamic)proxy).OnReceivedAsync(JsonConvert.DeserializeObject<dynamic>(data));
			return proxy;
		}
  
		private async Task NegotiateSession<TSession>(Func<TServiceContract, Task<TSession>> setup, Connection connection)
		{
			var interceptor = new SessionCreationInterceptor();
			dynamic factory = new ProxyFactory<TServiceContract>();
			factory.Register(interceptor);
			factory.CallingConstructor = new Ctor();
			var proxy = factory.Create();
			setup(proxy);

			var receiveTask = ReceiveAsync(connection);
			await connection.Send(interceptor.OperationDescriptor);
			await receiveTask;
		}

		private static Task<string> ReceiveAsync(Connection connection)
		{
			var tcs = new TaskCompletionSource<string>();
			Action<string> receiveHandler = null;
			receiveHandler = message =>
				{
					tcs.TrySetResult(message);
					connection.Received -= receiveHandler;
				};

			Action<Exception> errorHandler = null;
			errorHandler = error =>
				{
					tcs.TrySetException(error);
					connection.Error -= errorHandler;
				};

			connection.Received += receiveHandler;
			connection.Error += errorHandler;
			return tcs.Task;
		}
	}
}
