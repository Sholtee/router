/********************************************************************************
* RouterDelegateTests.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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
        private sealed class TestCaseDescriptor
        {
            public string[] Routes { get; set; } = null!;

            public Dictionary<string, Dictionary<string, object?>?> Cases { get; set; } = null!; 
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

        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                TestCaseDescriptor[] testCases = JsonSerializer.Deserialize<TestCaseDescriptor[]>
                (
                    File.ReadAllText("routerTestCases.json"),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    }
                )!;

                foreach (TestCaseDescriptor testCaseGroup in testCases)
                {
                    foreach (KeyValuePair<string, Dictionary<string, object?>?> testCase in testCaseGroup.Cases)
                    {
                        yield return new object?[] { testCaseGroup.Routes, testCase.Key, testCase.Value };
                    }
                }
            }
        }

        [TestCaseSource(nameof(TestCases))]
        public void DelegateShouldRoute(string[] routes, string input, Dictionary<string, object?>? paramz)
        {
            object
                request = new(),
                userData = new();

            Mock<DefaultHandler<object, object?, object>> mockDefaultHandler = new(MockBehavior.Strict);
            mockDefaultHandler
                .Setup(h => h.Invoke(request, userData, input))
                .Returns(true);

            Mock<Handler<object, object?, object>> mockHandler = new(MockBehavior.Strict);
            mockHandler
                .Setup(h => h.Invoke(request, It.Is<IReadOnlyDictionary<string, object?>>(actual => ValidArgs(actual, paramz)), userData, input))
                .Returns(true);

            RouterBuilder<object, object, object> bldr = new(mockDefaultHandler.Object, DefaultConverters.Instance);
            foreach (string route in routes)
            {
                bldr.AddRoute(route, mockHandler.Object);
            }
            Router<object, object?, object> router = bldr.Build();
            
            Assert.DoesNotThrow(() => router(request, userData, input));
            if (paramz is null)
                mockDefaultHandler.Verify(h => h.Invoke(request, userData, input), Times.Once);
            else
                mockHandler.Verify(h => h.Invoke(request, It.IsAny<IReadOnlyDictionary<string, object?>>(), userData, input));
        }
    }
}