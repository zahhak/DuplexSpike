using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common;

namespace LongRunningSignalR
{
	public class ConnectionContext<TService, TSession, TCallback> : IConnectionContext<TService, TSession, TCallback>
		where TSession : IOperationSession<TCallback>
	{
		private readonly ConcurrentDictionary<string, Lazy<TSession>> sessions;
		private readonly ConcurrentDictionary<string, object> pendingRequests;

		public ConcurrentDictionary<string, object> PendingRequests
		{
			get
			{
				return this.pendingRequests;
			}
		}

		public ConnectionContext()
		{
			this.pendingRequests = new ConcurrentDictionary<string, object>();
			this.sessions = new ConcurrentDictionary<string, Lazy<TSession>>();
		}

		public TSession GetOrCreateSession(string connectionId, TService service, MethodInfo methodInfo,
			IDictionary<string, object> arguments, out bool didCreateSession)
		{
			var session = this.sessions.GetOrAdd(connectionId, _ => 
				new Lazy<TSession>(() => (TSession)methodInfo.Invoke(service, arguments.Values.ToArray())));
			didCreateSession = !session.IsValueCreated;
			return session.Value;
		}
	}
}