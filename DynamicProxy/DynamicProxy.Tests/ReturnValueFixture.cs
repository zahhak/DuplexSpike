using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.DynamicProxy.Abstraction;
using Xunit;

namespace Telerik.DynamicProxy.Tests
{
    public class ReturnValueFixture
    {
        [Fact]
        public void ShouldInterceptorForMethodWithReturn()
        {
            var foo = Proxy.Create<IFoo>(x =>
            {
                 x.Register(new FooInterceptorThatSetsReturn(10));
            });

            Assert.Equal(foo.GetValue(100), 10);
        }

        public class FooInterceptorThatSetsReturn : IInterceptor
        {
            public FooInterceptorThatSetsReturn(int expectedReturnValue)
            {
                this.returnValue = expectedReturnValue;
            }

            public void Intercept(IInvocation invocation)
            {
                invocation.SetReturn(returnValue);
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            private int returnValue;
        }


        public interface IFoo
        {
            void Execute();
            int GetValue(int arg1);
            string[] GetStrings(string arg1, string arg2);
            Guid GetGuid();
        }
    }
}
