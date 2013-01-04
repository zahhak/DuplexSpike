using System;
using System.Linq;
using System.Threading.Tasks;
using Common;

namespace LongRunningSignalR.Services
{
	public class BuildService : IBuildContract
	{
		public IBuildSession Build()
		{
			return new BuildSession();
		}
	}
}