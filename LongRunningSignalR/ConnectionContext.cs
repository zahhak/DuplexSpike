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
		private readonly ConcurrentDictionary<string, TSession> sessions;
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
			this.sessions = new ConcurrentDictionary<string, TSession>();
		}

		//LAZY
		public TSession GetOrCreateSession(string connectionId, TService service, MethodInfo methodInfo,
			IDictionary<string, object> arguments, out bool didCreateSession)
		{
			bool didCreateSessionInternal = false;
			var session = this.sessions.GetOrAdd(connectionId, _ => this.CreateSession(service, arguments, methodInfo, out didCreateSessionInternal));
			didCreateSession = didCreateSessionInternal;
			return session;
		}

		private TSession CreateSession(TService service, IDictionary<string, object> arguments, MethodInfo methodInfo, out bool didCreateSession)
		{
			var session = (TSession)methodInfo.Invoke(service, arguments.Values.ToArray());
			didCreateSession = true;
			return session;
		}
	}
}