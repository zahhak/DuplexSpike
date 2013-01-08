using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.DynamicProxy.Abstraction;
using Xunit;
using System.Reflection;

namespace Telerik.DynamicProxy.Tests
{
    public class OutputArgFixture
    {
        [Fact]
        public void ShouldAssertOuputArguments()
        {
            var fooInterceptor = new FooInterceptor(delegate(IInvocation invocation)
            {
                invocation.Arguments[0].Value = 5;
                invocation.Arguments[1].Value = "a";
                invocation.Arguments[3].Value = "b";
            });

            IFoo foo = Proxy.Create<IFoo>(x => {
                x.Register(fooInterceptor);
            });

            int acutalInt = 3;
            string actualStr1 = "x";
            string actualStr2;
            
            foo.Execute(out acutalInt, ref actualStr1, 1, out actualStr2);

            Assert.Equal(5, acutalInt);
            Assert.Equal("a", actualStr1);
            Assert.Equal("b", actualStr2);
        }

        public class FooInterceptor : IInterceptor
        {

            public FooInterceptor(Invoked invoked)
            {
                this.invoked = invoked;
            }

            public void Intercept(IInvocation invocation)
            {
                invoked(invocation);
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            private Invoked invoked;
            public delegate void Invoked(IInvocation invocation);
        }

        public interface IFoo
        {
            void Execute(out int arg1, ref string strArg1, int inArg, out string strArg2 );
        }

    }
}
