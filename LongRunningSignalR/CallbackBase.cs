using System;
using System.Linq;
using System.Threading.Tasks;

namespace LongRunningSignalR
{
    public abstract class CallbackBase
    {
        internal readonly ICallbackContext CallbackContext;

        public CallbackBase(ICallbackContext callbackContext)
        {
            this.CallbackContext = callbackContext;
        }
    }

    public interface ICallbackContext
    {
        uint GetNextOperationId();
    }
}