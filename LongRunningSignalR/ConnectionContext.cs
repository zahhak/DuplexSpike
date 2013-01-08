using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Common;

namespace LongRunningSignalR
{
	internal class ConnectionContext<TService, TSession, TCallback> where TSession : IOperationSession<TCallback>
	{
		private readonly ConcurrentDictionary<string, Lazy<Task<TSession>>> sessions;
		private readonly ConcurrentDictionary<string, ActionExecutor> sessionExecutors;
		private readonly ConcurrentDictionary<string, ActionExecutor> executors;
		private readonly ConcurrentDictionary<string, TCallback> callbacks;

		public ConnectionContext()
		{
			this.sessions = new ConcurrentDictionary<string, Lazy<Task<TSession>>>();
			this.executors = new ConcurrentDictionary<string, ActionExecutor>();
			this.sessionExecutors = new ConcurrentDictionary<string, ActionExecutor>();
			this.callbacks = new ConcurrentDictionary<string, TCallback>();
		}

		public bool TryGetCallback(string connectionId, out TCallback callback)
		{
			return this.callbacks.TryGetValue(connectionId, out callback);
		}

		public void AddCallback(string connectionId, TCallback callback)
		{
			this.callbacks.TryAdd(connectionId, callback);
		}

		public async void DisposeSession(string connectionId)
		{
			Lazy<Task<TSession>> lazySession;
			if (this.sessions.TryRemove(connectionId, out lazySession) && lazySession.IsValueCreated)
			{
				(await lazySession.Value).Dispose();
			}

			TCallback callback;
			this.callbacks.TryRemove(connectionId, out callback);
		}

		public Task<TSession> GetOrCreateSessionAsync(string connectionId, TService service, MethodInfo methodInfo,
			IDictionary<string, object> arguments, out bool didCreateSession)
		{
			Func<Task<TSession>> getOrCreateSession = () => this.sessionExecutors.GetOrAdd(methodInfo.Name, __ => new ActionExecutor(methodInfo)).Execute(service, arguments).CastFromObject<TSession>();
			var session = this.sessions.GetOrAdd(connectionId, _ => new Lazy<Task<TSession>>(getOrCreateSession));
			didCreateSession = !session.IsValueCreated;
			return session.Value;
		}

		public ActionExecutor GetOrCreateExecutor(MethodInfo methodInfo)
		{
			return this.executors.GetOrAdd(methodInfo.Name, _ => new ActionExecutor(methodInfo));
		}
	}
}