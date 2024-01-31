/********************************************************************************
* RouterDelegateTests.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if NETFRAMEWORK
using System.Linq;
#endif
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Properties;

    [TestFixture]
    public class RouterDelegateTests
    {
        private sealed class RoutingTestCaseDescriptor
        {
            public IReadOnlyDictionary<string, string> Routes { get; set; } = null!;

            public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>?> Cases { get; set; } = null!;
        }

        private static bool ValidArgs(IReadOnlyDictionary<string, object?> actual, IReadOnlyDictionary<string, object?>? expected)
        {
            if (expected is null) // we cannot reach here otherwise
                return false;

            if (actual.Count != expected.Count)
                return false;

            foreach (KeyValuePair<string, object?> param in expected)
            {
                if (!actual.ContainsKey(param.Key) || actual[param.Key]?.ToString() != param.Value?.ToString())
                    return false;
            }

            return true;
        }

        public static IEnumerable<object?[]> RoutingTestCases
        {
            get
            {
                RoutingTestCaseDescriptor[] testCases = JsonSerializer.Deserialize<RoutingTestCaseDescriptor[]>
                (
                    File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "routerTestCases.json")),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    }
                )!;

                foreach (RoutingTestCaseDescriptor testCaseGroup in testCases)
                {
                    foreach (KeyValuePair<string, IReadOnlyDictionary<string, object?>?> testCase in testCaseGroup.Cases)
                    {
                        yield return new object?[] { testCaseGroup.Routes, testCase.Key, testCase.Value };
                    }
                }
            }
        }

        [TestCaseSource(nameof(RoutingTestCases))]
        public void DelegateShouldRoute(IReadOnlyDictionary<string, string> routes, string input, IReadOnlyDictionary<string, object?>? expectedParams)
        {
            object
                request = new(),
                userData = new();

            Mock<DefaultRequestHandler> mockDefaultHandler = new(MockBehavior.Strict);
            mockDefaultHandler
                .Setup(h => h.Invoke(userData, HttpStatusCode.NotFound))
                .Returns(true);

            Mock<RequestHandler> mockHandler = new(MockBehavior.Strict);
            mockHandler
                .Setup(h => h.Invoke(It.IsAny<IReadOnlyDictionary<string, object?>>(), userData))
                .Returns(true);

            RouterBuilder bldr = new(mockDefaultHandler.Object, DefaultConverters.Instance);
            foreach (KeyValuePair<string, string> route in routes)
            {
                bldr.AddRoute(route.Key, handler: (actualParams, userData) =>
                {
                    Assert.That(actualParams, Is.Not.Null);

                    return mockHandler.Object
                    (
#if NETFRAMEWORK
                        actualParams.Append(new KeyValuePair<string, object?>("@callback", route.Value)).ToDictionary(k => k.Key, k => k.Value),
#else
                        new Dictionary<string, object?>(actualParams) { { "@callback", route.Value } },
#endif
                        userData
                    );
                });
            }
            Router router = bldr.Build();

            Assert.DoesNotThrow(() => router(userData, input));
            if (expectedParams is null)
            {
                mockDefaultHandler.Verify(h => h.Invoke(userData, HttpStatusCode.NotFound), Times.Once);
                mockDefaultHandler.VerifyNoOtherCalls();
                mockHandler.VerifyNoOtherCalls();
            }
            else
            {
                mockHandler.Verify(h => h.Invoke(It.Is<IReadOnlyDictionary<string, object?>>(actualParams => ValidArgs(actualParams, expectedParams)), userData), Times.Once);
                mockHandler.VerifyNoOtherCalls();
                mockDefaultHandler.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void DelegateShouldThrowOnNullPath()
        {
            Router router = new RouterBuilder(handler: (_, _) => false, DefaultConverters.Instance).Build();
            Assert.Throws<ArgumentNullException>(() => router(null, null!));
        }

        [Test]
        public void DelegateShouldThrowOnNullMethod()
        {
            Router router = new RouterBuilder(handler: (_, _) => false, DefaultConverters.Instance).Build();
            Assert.Throws<ArgumentNullException>(() => router(null, "path", null!));
        }

        [Test]
        public void DelegateShouldThrowOnUnregisteredRoute()
        {
            Router router = new RouterBuilder().Build();
            Assert.Throws<InvalidOperationException>(() => router(null, "/cica"), Resources.ROUTE_NOT_REGISTERED);
        }

        [Test]
        public void DelegateShouldHandleExceptions()
        {
            object userData = new();

            Exception ex = new();

            Mock<ExceptionHandler<Exception>> mockExceptionHandler = new(MockBehavior.Strict);
            mockExceptionHandler
                .Setup(h => h.Invoke(userData, ex))
                .Returns(true);

            Mock<RequestHandler> mockHandler = new(MockBehavior.Strict);
            mockHandler
                .Setup(h => h.Invoke(It.IsAny<IReadOnlyDictionary<string, object?>>(), userData))
                .Throws(ex);

            RouterBuilder bldr = new();
            bldr.AddRoute("/fail", mockHandler.Object);
            bldr.RegisterExceptionHandler(mockExceptionHandler.Object);

            Router router = bldr.Build();

            Assert.That(router(userData, "/fail") is true);
            mockExceptionHandler.Verify(h => h.Invoke(userData, ex), Times.Once);
            mockExceptionHandler.VerifyNoOtherCalls();
        }

        [Test]
        public void DelegateShouldHandleMultipleExceptions()
        {
            object userData = new();

            ArgumentException ex = new();

            Mock<ExceptionHandler<Exception>> mockExceptionHandler = new(MockBehavior.Strict);
            mockExceptionHandler
                .Setup(h => h.Invoke(userData, It.IsAny<Exception>()))
                .Returns(true);

            Mock<ExceptionHandler<ArgumentException>> mockArgExceptionHandler = new(MockBehavior.Strict);
            mockArgExceptionHandler
                .Setup(h => h.Invoke(userData, ex))
                .Returns(true);

            Mock<RequestHandler> mockHandler = new(MockBehavior.Strict);
            mockHandler
                .Setup(h => h.Invoke(It.IsAny<IReadOnlyDictionary<string, object?>>(), userData))
                .Throws(ex);

            RouterBuilder bldr = new();
            bldr.AddRoute("/fail", mockHandler.Object);
            bldr.RegisterExceptionHandler(mockArgExceptionHandler.Object);
            bldr.RegisterExceptionHandler(mockExceptionHandler.Object);

            Router router = bldr.Build();

            Assert.That(router(userData, "/fail") is true);
            mockExceptionHandler.VerifyNoOtherCalls();
            mockArgExceptionHandler.Verify(h => h.Invoke(userData, ex), Times.Once);
            mockArgExceptionHandler.VerifyNoOtherCalls();
        }

        [Test]
        public void DelegateShouldHandleUnknownMethods()
        {
            object
                request = new(),
                userData = new();

            Mock<DefaultRequestHandler> mockDefaultHandler = new(MockBehavior.Strict);
            mockDefaultHandler
                .Setup(h => h.Invoke(userData, HttpStatusCode.MethodNotAllowed))
                .Returns(true);

            Mock<RequestHandler> mockHandler = new(MockBehavior.Strict);
            mockHandler
                .Setup(h => h.Invoke(It.IsAny<IReadOnlyDictionary<string, object?>>(), userData))
                .Returns(true);

            RouterBuilder bldr = new(mockDefaultHandler.Object, DefaultConverters.Instance);
            bldr.AddRoute("/cica", mockHandler.Object, "GET");

            Router router = bldr.Build();
            Assert.That(router(userData, "/cica", "POST") is true);
            mockDefaultHandler.Verify(h => h.Invoke(userData, HttpStatusCode.MethodNotAllowed), Times.Once);
        }
    }

    [TestFixture]
    public class RouterDelegateStressTests
    {
        [Test]
        public async Task ConductStressTest([Values(1, 5, 10, 100, 500)] int routeCount, [Values(true, false)] bool hasParams)
        {
            AsyncRouterBuilder bldr = AsyncRouterBuilder.Create();

            for (int i = 0; i < routeCount; i++)
            {
                int paramIndex = 0;
                string route = "/" + string.Join
                (
                    "/",
                    Enumerable
                        .Repeat("segment", i)
                        .Select((segment, i) => hasParams && i % 2 == 0 ? $"{{param{paramIndex++}:str}}" : segment + i)
                );

                int capture = i;

                bldr.AddRoute(route, handler: (paramz, _) => $"result{capture}");
            }

            AsyncRouter router = bldr.Build();

            for (int i = 0; i < routeCount; i++)
            {
                string route = "/" + string.Join
                (
                    "/",
                    Enumerable.Repeat("segment", i).Select((segment, i) => segment + i)
                );
                object? result = await router(null, route);

                int capture = i;

                Assert.That(result, Is.EqualTo($"result{capture}"));
            }
        }
    }
}