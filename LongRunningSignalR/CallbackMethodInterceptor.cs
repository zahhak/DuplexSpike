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
		private readonly Func<dynamic, object> sendFunc;
		private readonly Func<dynamic, Type, object> sendFuncGeneric;
        private readonly JsonSerializer serializer;

		public CallbackMethodInterceptor(Func<dynamic, object> sendFunc, Func<dynamic, Type, object> sendFuncGeneric, JsonSerializer serializer)
        {
            this.sendFunc = sendFunc;
			this.sendFuncGeneric = sendFuncGeneric;
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
            else if (typeof(Task) == invocation.Method.ReturnType)
            {
                invocation.SetReturn(this.sendFunc(this.OperationDescriptor));
                return;
            }
            else if (typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
            {
                var innerType = invocation.Method.ReturnType.GetGenericArguments().Single();
				invocation.SetReturn(sendFuncGeneric(this.OperationDescriptor, innerType));
            }
            else
            {
                throw new InvalidOperationException(string.Format("Unsupported method: {0}", invocation.Method));
            }
        }
    }
}