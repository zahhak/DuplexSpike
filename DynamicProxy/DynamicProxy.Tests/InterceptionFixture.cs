using System;
using System.Linq;
using Telerik.DynamicProxy.Abstraction;
using Xunit;
using System.IO;

namespace Telerik.DynamicProxy.Tests
{
    public class InterceptionFixture
    {
        [Fact]
        public void ShouldAssertInterception()
        {
            var interceptor = new FooInterceptor();

            interceptor.OnExecute += delegate(IInvocation s)
            {
                Assert.Equal("Execute", s.Method.Name);
            };

            var foo = Proxy.Create<IFoo>(interceptor);

            foo.Execute();
        }

        [Fact]
        public void ShouldAbleToInterceptFrameWorkClass()
        {
            var stream = Proxy.Create<Stream>(new FooInterceptor());
            Assert.NotNull(stream);
        }

        [Fact]
        public void ShouldAssertObjectOverridesInterception()
        {
            var foo = Proxy.Create<Foo>(x => { x.IncludeObjectOverrides();});
            Assert.Equal(0, foo.GetHashCode()); 
        }

        [Fact]
        public void ShouldInterceptGenericMethodWithValueTypeConstraint()
        {
            var something = Proxy.Create<ISomething<int>>(new FooInterceptor());
            // should not crash.
            something.DoSomething<int>();
        }

        [Fact]
        public void ShouldNotCallNextInterceptorIfNotInstructed()
        {
           var interceptor1 = new FooInterceptor();
           var interceptor2 = new FooInterceptor();

            bool interceptor1Called = false;
            bool interceptor2Called = false;

           interceptor1.OnExecute += delegate {
               interceptor1Called = true;
           };

           interceptor2.OnExecute += delegate {
               interceptor2Called = true;
           };

            var foo = Proxy.Create<IFoo>(x =>
            {
                x.Register(interceptor1);
                x.Register(interceptor2);
            });


            foo.Execute();

            Assert.True(interceptor1Called);
            Assert.False(interceptor2Called);

        }
      

        public class Something<T> : ISomething<T>
        {

            #region ISomething<T> Members

            public void DoSomething<U>() where U : T
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        [Fact]
        public void ShouldAssertDefaultReturn()
        {
            var foo = Proxy.Create<IFoo>(new FooInterceptor());
            Assert.Equal(foo.GetValue(0), default(int));
        }

        [Fact]
        public void ShouldAssertDefaultGuidAsReturn()
        {
            var foo = Proxy.Create<IFoo>(new FooInterceptor());
            Assert.Equal(foo.GetGuid(), default(Guid));
        }

        [Fact]
        public void ShouldAssertDefaultArrayAsReturn()
        {
            var foo = Proxy.Create<IFoo>(new FooInterceptor());
            Assert.Equal(foo.GetStrings("x", "y").Length, 0);
        }

     
        [Fact]
        public void ShoulAssertNonDefaultConstructor()
        {
            var factory = new ProxyFactory(typeof(Foo));

            factory.Register(new FooInterceptor());
            factory.CallingConstructor = new Ctor(10, 10);
           
            var foo = factory.Create();

            Assert.NotNull(foo);
        }

        #if  !SILVERLIGHT

        [Fact]
        public void ShouldAssertProxyFromNonInstanceClass()
        {
            var executor = new FooInterceptor();

            var called = false;

            executor.OnExecute += delegate {
                called = true;
            };
            var foo = Proxy.Create<FooInternal>(executor);

            foo.Execute();

            Assert.True(called);
        }

        [Fact]
        public void ShouldAssertNumberOfTimesMainInterceptorCalled()
        {
            var fooInterceptor = new FooInterceptor(true);

            int actualCount = 0;

            fooInterceptor.OnExecute += delegate
            {
                actualCount++;
            };

            var foo = Proxy.Create<Foo>(x =>
            {
                x.Register(fooInterceptor);
                x.Register(new LogInterceptor());
            });

            foo.Sum(1, 1);

            Assert.Equal(1, actualCount);
        }

        #endif

        [Fact]
        public void ShouldAssertBaseCall()
        {
            var foo = Proxy.Create<Foo>(new FooInterceptor(true));
            Assert.Equal(foo.Sum(1, 2), 3);
        }

        [Fact]
        public void ShouldAssertDeclaringType()
        {
            var fooInterceptor = new FooInterceptor();

            var foo = Proxy.Create<Foo>(fooInterceptor);

            fooInterceptor.OnExecute += delegate(IInvocation s)
            {
                Assert.Equal(s.DeclaringType.GetHashCode(), typeof(Foo).GetHashCode());
            };

            foo.Sum(1, 2);
        }

        [Fact]
        public void ShouldAssertTargetOfTheInvocation()
        {
            var fooInterceptor = new FooInterceptor();

            object target = null;

            var foo = Proxy.Create<IFoo>(x => x.Register(fooInterceptor));

           
            fooInterceptor.OnExecute += delegate(IInvocation invocation)
            {
                target = invocation.Target;
            };

            foo.Execute();

            Assert.Equal(foo.GetHashCode(), target.GetHashCode());
        }

        [Fact]
        public void ShouldAssertConstructorArguments()
        {
            var factory = new ProxyFactory<Foo>();

            var fooInterceptor = new FooInterceptor();
            fooInterceptor.OnSetup += delegate(object target, Argument[] args)
            {
                Assert.Equal(args[0].Value, 10);
                Assert.Equal(args[1].Value, 11);
            };

            factory.Register(fooInterceptor);
            factory.CallingConstructor = new Ctor(10, 11);

            var foo = factory.Create();

            Assert.NotNull(foo);
        }

        [Fact]
        public void ShouldAssertProxyFromAbstractClass()
        {
            var foo = Proxy.Create<FooAbstract>(new FooInterceptor());
            Assert.NotNull(foo);
        }

        [Fact]
        public void ShouldAssertGenericMethod()
        {
            var foo = Proxy.Create<FooGeneric>(new FooInterceptor(string.Empty));
            var result = foo.Get(string.Empty);
            Assert.Equal(result, string.Empty);
        }

        [Fact]
        public void ShouldAssertGenericMethodWithValueTypeArg()
        {
            var foo = Proxy.Create<FooGeneric>(new FooInterceptor());
            var result = foo.Get(0);
            Assert.Equal(result, 0);
        }

        [Fact]
        public void ShouldAssertVaryingGenericArguments()
        {
            var foo = Proxy.Create<FooGeneric>(new FooInterceptor(10));
            var result = foo.Get<int, int>(10);
            Assert.Equal(result, 10);
        }

        [Fact]
        public void ShouldAssertContinueOnGenericClass()
        {
            var foo = Proxy.Create<FooGeneric>(new FooInterceptor(true));
            Assert.Throws<ArgumentException>(() => foo.Get<int, int>(10));
        } 

        [Fact]
        public void ShouldAssertGenericMethodCallFromClassType()
        {
            var foo = Proxy.Create<FooGeneric2<int>>(new FooInterceptor());
            var result = foo.Get(0);
            Assert.Equal(result, 0);
        }

        [Fact]
        public void ShouldAssertGenericMethodWithNonGenericMethodDefintation()
        {
            var foo = Proxy.Create<FooGeneric<int>>(new FooInterceptor());
            var result = foo.Get(1, 1);
            Assert.Equal(result, 0);
        }

        [Fact]
        public void ShouldAssertNoDefaultConstructorClass()
        {
            var interceptor = new FooInterceptor();

            var foo = Proxy.Create<FooNonDefault>(x =>
            {
                x.Register(interceptor).CallConstructor("a");
            });

            Assert.NotNull(foo);
        }

        [Fact]
        public void ShouldAssertGenericOutGenericArg()
        {
            var interceptor = new FooInterceptor();

            var foo = Proxy.Create<FooGeneric>(x =>
            {
                x.Register(interceptor);
            });

            int result = 0;

            foo.Execute<int, int>(out result);
        }

        [Fact]
        public void ShouldAssertInterfaceWithGenricArguments()
        {
            var interceptor = new FooInterceptor();

            var executor = Proxy.Create<IExecutor<int>>(x =>
            {
                x.Register(interceptor);
            });

            executor.Echo(string.Empty);
        }

        [Fact]
        public void ShouldAssertContinueOnAIvocation()
        {
            LogInterceptor log = new LogInterceptor();

            bool called = false;

            log.OnExecuted += delegate
            {
                called = true;
            };

            var foo = Proxy.Create<Foo>(x =>
            {
                x.Register(new FooInterceptorMain());
                x.Register(log);
            });

            Assert.Equal(2, foo.Sum(1, 1));
            Assert.True(called);
        }

        [Fact]
        public void ShouldAssertProxyForSameInterceptor()
        {
            var fooInterceptor = new FooInterceptor();

            var proxy1 = Proxy.Create<Foo>(x => x.Register(fooInterceptor));
            var proxy2 = Proxy.Create<Foo>(x => x.Register(fooInterceptor));

            Assert.Same(proxy1.GetType(), proxy2.GetType());
        }

        [Fact]
        public void ShouldAssertProxyForSameSettings()
        {
            var fooInterceptor = new FooInterceptor();

            var proxy1 = Proxy.Create<Foo>(x => x.Register(fooInterceptor).IncludeObjectOverrides());
            var proxy2 = Proxy.Create<Foo>(x => x.Register(fooInterceptor).IncludeObjectOverrides());

            Assert.Same(proxy1.GetType(), proxy2.GetType());
        }


        [Fact]
        public void ShouldNotAssertProxyForDifferentSettings()
        {
            var fooInterceptor = new FooInterceptor();

            var proxy1 = Proxy.Create<Foo>(x => x.Register(fooInterceptor).IncludeObjectOverrides());
            var proxy2 = Proxy.Create<Foo>(x => x.Register(fooInterceptor));

            Assert.NotSame(proxy1.GetType(), proxy2.GetType());
        }


        [Fact]
        public void ProxyHavingSametInterceptorOrderingShouldBeSame()
        {
            var fooInterceptor = new FooInterceptor();
            var logInterceptor = new LogInterceptor();

            var proxy1 = Proxy.Create<Foo>(x => x.Register(fooInterceptor).Register(logInterceptor));
            var proxy2 = Proxy.Create<Foo>(x => x.Register(fooInterceptor).Register(logInterceptor));

            Assert.Same(proxy1.GetType(), proxy2.GetType());
        }

        [Fact]
        public void ProxyHavingDifferentInterceptorOrderingShouldNotBeSame()
        {
            var fooInterceptor = new FooInterceptor();
            var logInterceptor = new LogInterceptor();

            var proxy1 = Proxy.Create<Foo>(x => x.Register(fooInterceptor).Register(logInterceptor));
            var proxy2 = Proxy.Create<Foo>(x => x.Register(logInterceptor).Register(fooInterceptor));

            Assert.NotSame(proxy1.GetType(), proxy2.GetType());
        }

        public class LogInterceptor : IInterceptor
        {

            #region IInterceptor Members

            public void Intercept(IInvocation invocation)
            {
                if (OnExecuted != null)
                    OnExecuted();
                // go to next.
                invocation.Continue();
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public delegate void ExecutionHandler();
            public event ExecutionHandler OnExecuted;

            #endregion
        }

        public class FooInterceptorMain : IInterceptor
        {

            #region IInterceptor Members

            public void Intercept(IInvocation invocation)
            {
                invocation.Continue();
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            #endregion
        }


        [Fact]
        public void ShouldBeAbleToIdentityProxyUsingMarker()
        {
            var foo = Proxy.Create<Foo>(new FooInterceptor());

            bool markerEnabled = false;

            if (foo is IProxy)
            {
                markerEnabled = true;
            }

            Assert.True(markerEnabled);
        }

        [Fact]
        public void ShouldImplementUserDefinedInterface()
        {
            var foo = Proxy.Create<Foo>(x =>
            {
                x.Register(new FooInterceptor());
                x.Implement(typeof(IComparable));
            });

            var iCompare = foo as IComparable;

            Assert.Equal(iCompare.CompareTo(null), 0);
        }

        public class FooInterceptorWithContinue : IInterceptor
        {

            #region IInterceptor Members

            public void Intercept(IInvocation invocation)
            {
                invocation.Continue();
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            #endregion
        }


        public class FooArgs : EventArgs
        {
            public string Value { get; set; }
        }

        public interface IExecutor<T>
        {
            event EventHandler<FooArgs> Done;
            event EventHandler Executed;
            void Execute(T value);
            void Execute(string value);
            void Execute(string s, int i);
            void Execute(string s, int i, bool b);
            void Execute(string s, int i, bool b, string v);

            string Echo(string s);
        }

        public interface ISomething<T>
        {
            void DoSomething<U>() where U : T;
        }

        public class FooInternal
        {
            internal FooInternal()
            {

            }

            public virtual void Execute()
            {

            }
        }

        public abstract class  FooAbstract
        {
            public virtual void DoIt()
            {
                
            }
        }

        public class FooNonDefault
        {
            public FooNonDefault(string message)
            {
            }
        }

        public class FooGeneric<T>
        {
            public virtual T Get<T1, T2>(T1 p1, T2 p2)
            {
                return default(T);
            }
            public virtual void Execute<T1>(T1 arg)
            {
                throw new Exception();
            }

            public virtual void Execute1<T1>(T1 arg) where T1 : IFooGeneric
            {
                throw new Exception();
            }
        }

        public class FooGeneric : IFooGeneric
        {
            public virtual T1 Get<T1>(T1 arg)
            {
                return default(T1);
            }

            public virtual TRet Get<T1, TRet>(T1 arg1)
            {
                throw new ArgumentException("Argument exeception");
            }

            public virtual TRet Execute<T1, TRet>(out T1 arg)
            {
                arg = default(T1);
                return default(TRet);
            }
        }


        public interface IFooGeneric
        {

        }

        public class FooGeneric2<T>
        {
            public virtual T Get(T arg)
            {
               return default(T);
            }
        }
    
        public interface IFoo 
        {
            void Execute();
            int GetValue(int arg1);
            string[] GetStrings(string arg1, string arg2);
            Guid GetGuid();
        }

        public class Foo
        {
            public Foo()
            {
            }

            public Foo(int arg1 , int arg2)
            {
                
            }

            public void Execute()
            {
                throw new NotImplementedException();
            }

            public virtual int Sum(int arg1, int arg2)
            {
                return arg1 + arg2;
            }
        }

    
        public class FooInterceptor : IInterceptor
        {
            public FooInterceptor()
            {
                
            }

            public FooInterceptor(bool callBase)
            {
                this.callBase = callBase;
            }

            public FooInterceptor(object @return)
            {
                this.@return = @return;
            }

            public void Intercept(IInvocation invocation)
            {
                if (OnExecute != null)
                {
                    OnExecute(invocation);
                }

                if (callBase)
                {
                    invocation.Continue();
                }
                else
                {
                    invocation.SetReturn(@return);
                }
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            private bool callBase;
            private object @return;

            public delegate void ExecutionHandler(IInvocation invocation);
            public event ExecutionHandler OnExecute;

            public delegate void SetupHandler(object target, Argument[] args);
            public event SetupHandler OnSetup;
        }
    }


}
