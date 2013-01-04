using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common;

namespace LongRunningSignalR
{
	public interface IConnectionContext<TService, TSession, TCallback>
        where TSession : IOperationSession<TCallback>
	{
		ConcurrentDictionary<string, object> PendingRequests { get; }

		TSession GetOrCreateSession(string connectionId, TService service, MethodInfo methodInfo, IDictionary<string, object> arguments, out bool didCreateSession);
	}
}