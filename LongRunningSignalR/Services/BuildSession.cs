using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace LongRunningSignalR.Services
{
	public class BuildSession : IBuildSession
	{
		internal IBuildClientCallback Callback { get; private set; }

		public BuildSession()
		{
		}

		public async Task<BuildResult> Build(BuildData buildData)
		{
			var agreement = await this.Callback.GetTermsAgreement();

			return new BuildResult { Shit = agreement };
		}

		public void Dispose()
		{

		}

		public void Initialize(IBuildClientCallback callback)
		{
			this.Callback = callback;
		}
	}
}