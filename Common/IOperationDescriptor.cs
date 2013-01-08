using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common
{
	public interface IOperationDescriptor
	{
		long OperationId { get; set; }

		string MethodName { get; set; }

		IDictionary<string, object> Parameters { get; set; }
	}
}