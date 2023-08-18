/********************************************************************************
* RouterDelegateTests.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;

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
                    File.ReadAllText("routerTestCases.json"),
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
                .Setup(h => h.Invoke(userData, input))
                .Returns(true);

            Mock<RequestHandler> mockHandler = new(MockBehavior.Strict);
            mockHandler
                .Setup(h => h.Invoke(It.IsAny<IReadOnlyDictionary<string, object?>>(), userData, input))
                .Returns(true);

            RouterBuilder bldr = new(mockDefaultHandler.Object, DefaultConverters.Instance);
            foreach (KeyValuePair<string, string> route in routes)
            {
                bldr.AddRoute(route.Key, (actualParams, userData, path) =>
                {
                    Assert.That(actualParams, Is.Not.Null);
                    return mockHandler.Object(new Dictionary<string, object?>(actualParams) { { "@callback", route.Value } }, userData, path);
                });
            }
            Router router = bldr.Build();

            Assert.DoesNotThrow(() => router(userData, input));
            if (expectedParams is null)
            {
                mockDefaultHandler.Verify(h => h.Invoke(userData, input), Times.Once);
                mockDefaultHandler.VerifyNoOtherCalls();
                mockHandler.VerifyNoOtherCalls();
            }
            else
            {
                mockHandler.Verify(h => h.Invoke(It.Is<IReadOnlyDictionary<string, object?>>(actualParams => ValidArgs(actualParams, expectedParams)), userData, input), Times.Once);
                mockHandler.VerifyNoOtherCalls();
                mockDefaultHandler.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void DelegateShouldThrowOnNullPath()
        {
            Router router = new RouterBuilder((_, _) => false, DefaultConverters.Instance).Build();
            Assert.Throws<ArgumentNullException>(() => router(null, null!));
        }
    }
}