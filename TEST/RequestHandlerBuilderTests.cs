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
            void VoidMethod(string para);

            Task<int> AsyncMethod(int para);

            int Method(int para);

            int RefMethod(ref int para);
        }


        [Test]
        public void GetCreateServiceArgumentShouldBeNullChecked()
        {
            RequestHandlerBuilder bldr = new(MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)));

            Assert.Throws<ArgumentNullException>(() => bldr.GetCreateServiceArgument(null!, null));
        }

        [Test]
        public void GetCreateServiceArgumentShouldThrowOnInvalidArgument()
        {
            RequestHandlerBuilder bldr = new(MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)));

            ParameterInfo para = MethodInfoExtractor.Extract<IServiceProvider>(p => p.GetService(null!)).ReturnParameter;

            Assert.Throws<NotSupportedException>(() => bldr.GetCreateServiceArgument(para, null));
        }

        [Test]
        public void GetCreateServiceArgumentShouldReflectTheActualService()
        {
            RequestHandlerBuilder bldr = new(MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)));

            ParameterInfo para = MethodInfoExtractor.Extract<IServiceProvider>(p => p.GetService(null!)).GetParameters()[0];

            Assert.That(Expression.Lambda<Func<Type>>(bldr.GetCreateServiceArgument(para, null)).Compile().Invoke(), Is.EqualTo(typeof(IMyService)));
        }

        [Test]
        public void GetInvokeServiceArgumentShouldBeNullChecked()
        {
            RequestHandlerBuilder bldr = new(MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)));

            Assert.Throws<ArgumentNullException>(() => bldr.GetInvokeServiceArgument(null!, RouteTemplate.Parse("/{para:int}"), null));
            Assert.Throws<ArgumentNullException>(() => bldr.GetInvokeServiceArgument(bldr.InvokeServiceMethod.GetParameters()[0], null!, null));
        }

        [Test]
        public void GetInvokeServiceArgumentShouldThrowOnRefMethod()
        {
            RequestHandlerBuilder bldr = new(MethodInfoExtractor.Extract<IMyService, int>((svc, val) => svc.RefMethod(ref val)));

            ParameterInfo para = bldr.InvokeServiceMethod.GetParameters()[0];

            Assert.Throws<ArgumentException>(() => bldr.GetInvokeServiceArgument(para, RouteTemplate.Parse("/{para:int}"), null), BY_REF_PARAMETER);
        }

        [Test]
        public void GetInvokeServiceArgumentShouldOnUndefinedParameter()
        {
            RequestHandlerBuilder bldr = new(MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)));

            ParameterInfo para = MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)).GetParameters()[0];

            Assert.Throws<ArgumentException>(() => bldr.GetInvokeServiceArgument(para, RouteTemplate.Parse("/"), null), PARAM_NOT_DEFINED);
        }

        [Test]
        public void InvokeServiceShouldBeNullChecked()
        {
            RequestHandlerBuilder bldr = new(MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)));

            Assert.Throws<ArgumentNullException>(() => bldr.InvokeService(null!, null));
        }

        [Test]
        public void CreateLambdaShouldBeNullChecked()
        {
            RequestHandlerBuilder bldr = new(MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)));

            Assert.Throws<ArgumentNullException>(() => bldr.CreateLambda(null!, null));
        }

        [Test]
        public void CreateLambdaShouldSupportRegularMethods()
        {
            RequestHandler<int> lambda = (RequestHandler<int>) new RequestHandlerBuilder(MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)))
                .CreateLambda(RouteTemplate.Parse("/{para:int}"), null)
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
            RequestHandler<object?> lambda = (RequestHandler<object?>) new RequestHandlerBuilder(MethodInfoExtractor.Extract<IMyService>(svc => svc.VoidMethod(null!)))
                .CreateLambda(RouteTemplate.Parse("/{para:str}"), null)
                .Compile();

            Mock<IMyService> mockService = new(MockBehavior.Strict);
            mockService.Setup(svc => svc.VoidMethod("cica"));

            Mock<IServiceProvider> mockServicePrivder = new(MockBehavior.Strict);
            mockServicePrivder
                .Setup(p => p.GetService(typeof(IMyService)))
                .Returns(mockService.Object);

            Mock<IReadOnlyDictionary<string, object?>> mockParamz = new(MockBehavior.Strict);
            mockParamz
                .Setup(p => p["para"])
                .Returns("cica");

            Assert.That(lambda(mockParamz.Object, mockServicePrivder.Object), Is.Null);

            mockServicePrivder.Verify(p => p.GetService(typeof(IMyService)), Times.Once);
            mockService.Verify(svc => svc.VoidMethod("cica"), Times.Once);
            mockParamz.Verify(p => p["para"], Times.Once);
        }

        private sealed class InjectorDotNetRequestHandlerBuilder : RequestHandlerBuilder
        {
            public InjectorDotNetRequestHandlerBuilder(MethodInfo invokeServiceMethod) : base(invokeServiceMethod)
            {
            }

            protected override MethodInfo CreateServiceMethod { get; } = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null));

            protected internal override Expression GetCreateServiceArgument(ParameterInfo param, object? userData)
            {
                if (param.Position is 1)
                    return Expression.Constant(null, typeof(string));

                return base.GetCreateServiceArgument(param, userData);
            }
        }

        [Test]
        public void BuilderCanBeCustomized()
        {
            RequestHandler<int> lambda = (RequestHandler<int>) new InjectorDotNetRequestHandlerBuilder(MethodInfoExtractor.Extract<IMyService>(svc => svc.Method(0)))
                .CreateLambda(RouteTemplate.Parse("/{para:int}"), null)
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