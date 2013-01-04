using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common
{
	public interface IOperationDescriptor
	{
		string OperationId { get; }

		string MethodName { get; set; }

		IDictionary<string, object> Parameters { get; set; }
	}
}