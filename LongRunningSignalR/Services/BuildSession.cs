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
			var callbackResult = await this.Callback.GetTermsAgreement();

			return new BuildResult { Shit = callbackResult };
		}

		public void Dispose()
		{

		}

		public Task Future
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void Initialize(IBuildClientCallback callback)
		{
			this.Callback = callback;
		}
	}
}