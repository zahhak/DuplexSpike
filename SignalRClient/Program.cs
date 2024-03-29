﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Common;

namespace SignalRClient
{
	partial class Program
	{
		static void Main(string[] args)
		{
			Duplex();
			Console.ReadLine();
		}
  
		private static async void Duplex()
		{
			var context = new HttpOperationContext();
			var executor = context.GetDuplexExecutor<IBuildContract>();
			using (var session = await executor.Execute(o => o.Build(), (IBuildClientCallback)new BuildCallback()))
			{
				try
				{
					var result = await session.Build(new BuildData());
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
		}

		private class BuildCallback : IBuildClientCallback
		{
			public Task DoSomething()
			{
				var tcs = new TaskCompletionSource<string>();
				tcs.SetResult("test");
				return tcs.Task;
			}

			public Task<string> GetTermsAgreement()
			{
				var tcs = new TaskCompletionSource<string>();
				tcs.SetResult("TermsAgreement");
				return tcs.Task;
			}
		}
	}
}
