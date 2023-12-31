/********************************************************************************
* RequestHandlerBuilderTests.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Router.Extensions.Tests
{
    using DI.Interfaces;
    using Primitives;

    using static Properties.Resources;

    [TestFixture]
    public class RequestHandlerBuilderTests
    {
        public interface IMyService
        {
            void VoidMethod();

            Task<int> AsyncMethod(int para);

            int Method(int para);

            T Generic<T>(int para);

            int RefMethod(ref int para);
        }

        public RequestHandlerBuilder DefaultBuilder { get; } = new RequestHandlerBuilder();

        [Test]
        public void GetCreateServiceArgumentShouldThrowOnInvalidArgument()
        {
            ParameterInfo para = MethodInfoExtractor.Extract<IServiceProvider>(p => p.GetService(null!)).ReturnParameter;

            Assert.Throws<NotSupportedException>(() => DefaultBuilder.GetCreateServiceArgument(para, typeof(IMyService), null));
        }

        [Test]
        public void GetCreateServiceArgumentShouldReflectTheActualService()
        {
            ParameterInfo para = MethodInfoExtractor.Extract<IServiceProvider>(p => p.GetService(null!)).GetParameters()[0];

            Assert.That(Expression.Lambda<Func<Type>>(DefaultBuilder.GetCreateServiceArgument(para, typeof(IMyService), null)).Compile().Invoke(), Is.EqualTo(typeof(IMyService)));
        }

        [Test]
        public void GetInvokeServiceArgumentShouldThrowOnRefMethod()
        {
            ParameterInfo para = MethodInfoExtractor.Extract<IMyService, int>((svc, val) => svc.RefMethod(ref val)).GetParameters()[0];

            Assert.Throws<ArgumentException>(() => DefaultBuilder.GetInvokeServiceArgument(para, RouteTemplate.Parse("/{para:int}"), null), BY_REF_PARAMETER);
        }

        [Test]
        public void GetInvokeServiceArgumentShouldThrowOnUndefinedParameter()
        {
            ParameterInfo para = MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)).GetParameters()[0];

            Assert.Throws<ArgumentException>(() => DefaultBuilder.GetInvokeServiceArgument(para, RouteTemplate.Parse("/"), null), PARAM_NOT_DEFINED);
        }

        [Test]
        public void CreateLambdaThrowOnOpenGenericMethod()
        {
            Assert.Throws<ArgumentException>(() => DefaultBuilder.CreateLambda(RouteTemplate.Parse("/{para:int}"), MethodInfoExtractor.Extract<IMyService>(svc => svc.Generic<int>(0)).GetGenericMethodDefinition(), null), INVALID_HANDLER);
            Assert.DoesNotThrow(() => DefaultBuilder.CreateLambda(RouteTemplate.Parse("/{para:int}"), MethodInfoExtractor.Extract<IMyService>(svc => svc.Generic<int>(0)), null));
        }

        [Test]
        public void CreateLambdaShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => DefaultBuilder.CreateLambda(null!, MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)), null));
            Assert.Throws<ArgumentNullException>(() => DefaultBuilder.CreateLambda(RouteTemplate.Parse("/"), null!, null));
        }

        [Test]
        public void CreateLambdaShouldSupportRegularMethods()
        {
            RequestHandler<int> lambda = (RequestHandler<int>) DefaultBuilder
                .CreateLambda(RouteTemplate.Parse("/{para:int}"), MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)), null)
                .Compile();

            Mock<IMyService> mockService = new(MockBehavior.Strict);
            mockService
                .Setup(svc => svc.Method(1986))
                .Returns(1990);

            Mock<IServiceProvider> mockServicePrivder = new(MockBehavior.Strict);
            mockServicePrivder
                .Setup(p => p.GetService(typeof(IMyService)))
                .Returns(mockService.Object);

            Mock<IReadOnlyDictionary<string, object?>> mockParamz = new(MockBehavior.Strict);
            mockParamz
                .Setup(p => p["para"])
                .Returns(1986);

            Assert.That(lambda(mockParamz.Object, mockServicePrivder.Object), Is.EqualTo(1990));

            mockServicePrivder.Verify(p => p.GetService(typeof(IMyService)), Times.Once);
            mockService.Verify(svc => svc.Method(1986), Times.Once);
            mockParamz.Verify(p => p["para"], Times.Once);
        }

        [Test]
        public void CreateLambdaShouldSupportVoidMethods()
        {
            RequestHandler<object?> lambda = (RequestHandler<object?>) DefaultBuilder
                .CreateLambda(RouteTemplate.Parse("/"), MethodInfoExtractor.Extract<IMyService>(svc => svc.VoidMethod()), null)
                .Compile();

            Mock<IMyService> mockService = new(MockBehavior.Strict);
            mockService.Setup(svc => svc.VoidMethod());

            Mock<IServiceProvider> mockServicePrivder = new(MockBehavior.Strict);
            mockServicePrivder
                .Setup(p => p.GetService(typeof(IMyService)))
                .Returns(mockService.Object);

            Mock<IReadOnlyDictionary<string, object?>> mockParamz = new(MockBehavior.Strict);

            Assert.That(lambda(mockParamz.Object, mockServicePrivder.Object), Is.Null);

            mockServicePrivder.Verify(p => p.GetService(typeof(IMyService)), Times.Once);
            mockService.Verify(svc => svc.VoidMethod(), Times.Once);
            mockParamz.VerifyNoOtherCalls();
        }

        private sealed class InjectorDotNetRequestHandlerBuilder : RequestHandlerBuilder
        {
            protected override MethodInfo CreateServiceMethod { get; } = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null));

            protected internal override Expression GetCreateServiceArgument(ParameterInfo param, Type serviceType, object? userData)
            {
                if (param.Position is 1)
                    return Expression.Constant(null, typeof(string));

                return base.GetCreateServiceArgument(param, serviceType, userData);
            }
        }

        [Test]
        public void BuilderCanBeCustomized()
        {
            RequestHandler<int> lambda = (RequestHandler<int>) new InjectorDotNetRequestHandlerBuilder()
                .CreateLambda(RouteTemplate.Parse("/{para:int}"), MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)), null)
                .Compile();

            Mock<IMyService> mockService = new(MockBehavior.Strict);
            mockService
                .Setup(svc => svc.Method(1986))
                .Returns(1990);

            Mock<IInjector> mockServicePrivder = new(MockBehavior.Strict);
            mockServicePrivder
                .Setup(p => p.Get(typeof(IMyService), null))
                .Returns(mockService.Object);

            Mock<IReadOnlyDictionary<string, object?>> mockParamz = new(MockBehavior.Strict);
            mockParamz
                .Setup(p => p["para"])
                .Returns(1986);

            Assert.That(lambda(mockParamz.Object, mockServicePrivder.Object), Is.EqualTo(1990));

            mockServicePrivder.Verify(p => p.Get(typeof(IMyService), null), Times.Once);
            mockService.Verify(svc => svc.Method(1986), Times.Once);
            mockParamz.Verify(p => p["para"], Times.Once);
        }
    }
}