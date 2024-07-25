/********************************************************************************
* AsyncRouterDelegateTests.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Moq;
    using Properties;

    [TestFixture]
    public class AsyncRouterDelegateTests
    {
        public static IEnumerable<(Action<string, AsyncRouterBuilder>, object?)> Cases
        {
            get
            {    
                // void type
                yield return ((route, builder) => builder.AddRoute(route, handler: (paramz, userData) => Task.CompletedTask), null);

                // by ref type
                object obj = new();
                yield return ((route, builder) => builder.AddRoute(route, handler: (paramz, userData) => Task.FromResult(obj)), obj);
                yield return ((route, builder) => builder.AddRoute(route, handler: (paramz, userData) => obj), obj);

                // value type
                yield return ((route, builder) => builder.AddRoute(route, handler: (paramz, userData) => Task.FromResult(1986)), 1986);
                yield return ((route, builder) => builder.AddRoute(route, handler: (paramz, userData) => 1986), 1986);
            }
        }

        [Test]
        public async Task DelegateShouldRoute([ValueSource(nameof(Cases))] (Action<string, AsyncRouterBuilder> Registrar, object? Epxected) @case)
        {
            AsyncRouterBuilder builder = AsyncRouterBuilder.Create(handler: (_, _) => false, DefaultConverters.Instance);
            @case.Registrar("/cica", builder);

            AsyncRouter router = builder.Build();

            Assert.That(await router(null, "/".AsSpan(), "GET".AsSpan()), Is.False);
            Assert.That(await router(null, "/cica".AsSpan(), "GET".AsSpan()), Is.EqualTo(@case.Epxected));
        }

        [Test]
        public void DelegateShouldThrowOnUnregisteredRoute()
        {
            AsyncRouter router = AsyncRouterBuilder.Create().Build();
            Assert.ThrowsAsync<InvalidOperationException>(async () => await router(null, "/cica".AsSpan(), "GET".AsSpan()), Resources.ROUTE_NOT_REGISTERED);
        }

        public static IEnumerable<object> Exceptions
        {
            get
            {
                yield return new Exception();
                yield return new ArgumentException();
            }
        }

        [TestCaseSource(nameof(Exceptions))]
        public async Task DelegateShouldHandleExceptions<TException>(TException ex) where TException : Exception
        {
            object userData = new();

            Mock<ExceptionHandler<TException, Task<bool>>> mockExceptionHandler = new(MockBehavior.Strict);
            mockExceptionHandler
                .Setup(h => h.Invoke(userData, ex))
                .Returns(Task.FromResult(true));

            Mock<RequestHandler<Task>> mockHandler = new(MockBehavior.Strict);
            mockHandler
                .Setup(h => h.Invoke(It.IsAny<IReadOnlyDictionary<string, object?>>(), userData))
                .Returns(Task.FromException(ex));

            AsyncRouterBuilder bldr = AsyncRouterBuilder.Create();
            bldr.AddRoute("/fail", mockHandler.Object);
            bldr.RegisterExceptionHandler(mockExceptionHandler.Object);

            AsyncRouter router = bldr.Build();

            Assert.That(await router(userData, "/fail".AsSpan(), "GET".AsSpan()) is true);
            mockExceptionHandler.Verify(h => h.Invoke(userData, ex), Times.Once);
            mockExceptionHandler.VerifyNoOtherCalls();
        }
    }
}