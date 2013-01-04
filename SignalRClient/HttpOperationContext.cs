using System;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRClient
{
	public class HttpOperationContext : IHttpOperationContext
	{
		public async Task<IOperationExecutor<TOperation>> GetDuplexExecutor<TOperation, TCallback>(TCallback callback)
		{
			var duplexExecutor = new DuplexOperationExecutor<TOperation, TCallback>(this, callback);
			await duplexExecutor.Initialize();
			return duplexExecutor;
		}
	}
}
