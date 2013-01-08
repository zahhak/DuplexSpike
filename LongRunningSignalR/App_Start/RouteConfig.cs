using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Common;
using Microsoft.AspNet.SignalR;

namespace LongRunningSignalR
{
	public class RouteConfig
	{
		public static void RegisterRoutes(RouteCollection routes)
		{
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapConnection("myConnection", "duplex/build/{*operation}",
				typeof(ServiceConnection<IBuildContract, IBuildSession, IBuildClientCallback>),
				new DependencyResolver<IBuildContract, IBuildSession, IBuildClientCallback>(GlobalHost.DependencyResolver));
		}
	}
}