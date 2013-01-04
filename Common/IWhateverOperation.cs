using System;
using System.Linq;
using System.Threading.Tasks;

namespace Common
{
	public interface IWhateverSession : IOperationSession<IClientCallback>
	{
		Task BarAsync();
	}

	public interface IWhateverSession2 : IOperationSession<object, IClientCallback>
	{
		void Baz();
	}

	public interface IWhateverOperation
	{
		IWhateverSession DoWhatever();

		IWhateverSession2 DoWhatever2();
	}

	public interface IClientCallback
	{
		Task<int> FooAsync();
	}
}