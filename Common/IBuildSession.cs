using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common;
using System.Threading.Tasks;

namespace Common
{
	public interface IBuildSession : IOperationSession<IBuildClientCallback>
	{
		Task<BuildResult> Build(BuildData buildData);
	}
}