﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common;
using LongRunningSignalR.Services;
using Microsoft.AspNet.SignalR;

namespace LongRunningSignalR
{
	public class DependencyResolver<TService, TSession, TCallback> : DefaultDependencyResolver where TSession : IOperationSession<TCallback>
	{
		//Import ServiceRegistry maybe?
		private Lazy<ConnectionContext<TService, TSession, TCallback>> connectionContext =
			new Lazy<ConnectionContext<TService, TSession, TCallback>>(() => new ConnectionContext<TService, TSession, TCallback>());
		
		public override object GetService(Type serviceType)
		{
			if (serviceType == typeof(IConnectionContext<TService, TSession, TCallback>))
			{
				return connectionContext.Value;
			}

			if (serviceType == typeof(IBuildContract))
			{
				return new BuildService();
			}

			return base.GetService(serviceType);
		}


	}
}