using System.Threading.Tasks;

namespace SignalRClient
{
	public interface IHttpOperationContext
	{
		Task<IOperationExecutor<TOperation>> GetDuplexExecutor<TOperation, TCallback>(TCallback callback);
	}
}