using System;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRClient
{
	public class HttpOperationContext
	{
		public DuplexOperationExecutor<TServiceContract> GetDuplexExecutor<TServiceContract>()
		{
			return new DuplexOperationExecutor<TServiceContract>();
		}
	}
}
