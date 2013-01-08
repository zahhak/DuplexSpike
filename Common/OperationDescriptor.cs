using System;
using System.Collections.Generic;

namespace Common
{
	// back to OperationDescriptor, because dynamic can't be passed to base constructor
	public class OperationDescriptor : IOperationDescriptor
	{
		public long OperationId { get; set; }

		public string MethodName { get; set; }

		public IDictionary<string, object> Parameters { get; set; }
	}
}
