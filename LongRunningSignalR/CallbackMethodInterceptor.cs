using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Common;
using Newtonsoft.Json;
using Telerik.DynamicProxy;
using Telerik.DynamicProxy.Abstraction;
using Newtonsoft.Json.Linq;

namespace LongRunningSignalR
{
	public class CallbackMethodInterceptor : OperationDescriptorInterceptor
    {
		private readonly Func<dynamic, Type, object> sendFunc;
        private readonly JsonSerializer serializer;

		public CallbackMethodInterceptor(Func<dynamic, Type, object> sendFunc, JsonSerializer serializer)
        {
			this.sendFunc = sendFunc;
            this.serializer = serializer;
        }

		//SOMEWHAT DUPLICATED CODE ON CLIENT
        public override void Intercept(IInvocation invocation)
        {
			base.Intercept(invocation);

            if (invocation.Method.ReturnType == null || invocation.Method.ReturnType == typeof(void))
            {
                return;
            }
            else if (typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
            {
                var innerType = invocation.Method.ReturnType.GetGenericArguments().SingleOrDefault();
				if (innerType == null)
				{
					innerType = typeof(object);
				}
				invocation.SetReturn(sendFunc(this.OperationDescriptor, innerType));
            }
            else
            {
                throw new InvalidOperationException(string.Format("Unsupported method: {0}", invocation.Method));
            }
        }
    }
}		