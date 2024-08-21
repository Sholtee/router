/********************************************************************************
* ModuleRegistrationTests.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Extensions;

    [TestFixture]
    public class ModuleRegistrationTests
    {
        public interface IModule
        {
            void Method();
            void Method(int p1);
            void Method(int p1, int p2);
            void Method(int p1, int p2, int p3);
            void Method(int p1, int p2, int p3, int p4);
            void Method(int p1, int p2, int p3, int p4, int p5);

            int Func();
            int Func(int p1);
            int Func(int p1, int p2);
            int Func(int p1, int p2, int p3);
            int Func(int p1, int p2, int p3, int p4);
            int Func(int p1, int p2, int p3, int p4, int p5);
        }

        public static IEnumerable<Action<ModuleRegistration<IModule>>> Registrations
        {
            get
            {
                yield return m => m.AddRoute("/", m => m.Method);
                yield return m => m.AddRoute<int>("/", m => m.Method);
                yield return m => m.AddRoute<int, int>("/", m => m.Method);
                yield return m => m.AddRoute<int, int, int>("/", m => m.Method);
                yield return m => m.AddRoute<int, int, int, int>("/", m => m.Method);
                yield return m => m.AddRoute<int, int, int, int, int>("/", m => m.Method);
                yield return m => m.AddRoute<int>("/", m => m.Func);
                yield return m => m.AddRoute<int, int>("/", m => m.Func);
                yield return m => m.AddRoute<int, int, int>("/", m => m.Func);
                yield return m => m.AddRoute<int, int, int, int>("/", m => m.Func);
                yield return m => m.AddRoute<int, int, int, int, int>("/", m => m.Func);
                yield return m => m.AddRoute<int, int, int, int, int, int>("/", m => m.Func);
            }
        }

        [TestCaseSource(nameof(Registrations))]
        public void AddRoute_ShouldBindTheModuleMethodToTheGivenRoute(Action<ModuleRegistration<IModule>> registration)
        {
            Expression<RequestHandler<object?>> handler = (_, _) => null;

            Mock<RequestHandlerBuilder> mockHandlerBuilder = new(MockBehavior.Strict);
            mockHandlerBuilder
                .Setup(hb => hb.CreateFactory(It.Is<ParsedRoute>(r => r.Template == "/"), It.IsAny<MethodInfo>(), null))
                .Returns(handler);

            Mock<IRouterBuilder> mockRouterBuilder = new(MockBehavior.Strict);
            mockRouterBuilder
                .Setup(b => b.AddRoute(It.Is<ParsedRoute>(r => r.Template == "/"), handler, new string[0]));

            ModuleRegistration<IModule> moduleRegistration = new(mockRouterBuilder.Object, mockHandlerBuilder.Object);
            registration(moduleRegistration);

            mockRouterBuilder.Verify(b => b.AddRoute(It.Is<ParsedRoute>(r => r.Template == "/"), handler, new string[0]), Times.Once);
        }

        public static IEnumerable<Action<ModuleRegistration<IModule>>> InvalidRegistrations
        {
            get
            {
                yield return m => m.AddRoute("/", m => null!);
                yield return m => m.AddRoute<int>("/", m => null!);
                yield return m => m.AddRoute<int, int>("/", m => null!);
                yield return m => m.AddRoute<int, int, int>("/", m => null!);
                yield return m => m.AddRoute<int, int, int, int>("/", m => null!);
                yield return m => m.AddRoute<int, int, int, int, int>("/", m => null!);
                yield return m => m.AddRoute<int>("/", m => null!);
                yield return m => m.AddRoute<int, int>("/", m => null!);
                yield return m => m.AddRoute<int, int, int>("/", m => null!);
                yield return m => m.AddRoute<int, int, int, int>("/", m => null!);
                yield return m => m.AddRoute<int, int, int, int, int>("/", m => null!);
                yield return m => m.AddRoute<int, int, int, int, int, int>("/", m => null!);

                yield return m => m.AddRoute(null!, m => m.Method);
                yield return m => m.AddRoute<int>(null!, m => m.Method);
                yield return m => m.AddRoute<int, int>(null!, m => m.Method);
                yield return m => m.AddRoute<int, int, int>(null!, m => m.Method);
                yield return m => m.AddRoute<int, int, int, int>(null!, m => m.Method);
                yield return m => m.AddRoute<int, int, int, int, int>(null!, m => m.Method);
                yield return m => m.AddRoute<int>(null!, m => m.Func);
                yield return m => m.AddRoute<int, int>(null!, m => m.Func);
                yield return m => m.AddRoute<int, int, int>(null!, m => m.Func);
                yield return m => m.AddRoute<int, int, int, int>(null!, m => m.Func);
                yield return m => m.AddRoute<int, int, int, int, int>(null!, m => m.Func);
                yield return m => m.AddRoute<int, int, int, int, int, int>(null!, m => m.Func);

                yield return m => m.AddRoute("/", null!);
                yield return m => m.AddRoute<int>("/", null!);
                yield return m => m.AddRoute<int, int>("/", null!);
                yield return m => m.AddRoute<int, int, int>("/", null!);
                yield return m => m.AddRoute<int, int, int, int>("/", null!);
                yield return m => m.AddRoute<int, int, int, int, int>("/", null!);
                yield return m => m.AddRoute<int>("/", null!);
                yield return m => m.AddRoute<int, int>("/", null!);
                yield return m => m.AddRoute<int, int, int>("/", null!);
                yield return m => m.AddRoute<int, int, int, int>("/", null!);
                yield return m => m.AddRoute<int, int, int, int, int>("/", null!);
                yield return m => m.AddRoute<int, int, int, int, int, int>("/", null!);
            }
        }

        [TestCaseSource(nameof(InvalidRegistrations))]
        public void AddRoute_ShouldThrowOnInvalidRegistration(Action<ModuleRegistration<IModule>> registration)
        {
            Mock<RequestHandlerBuilder> mockHandlerBuilder = new(MockBehavior.Strict);
            Mock<IRouterBuilder> mockRouterBuilder = new(MockBehavior.Strict);

            ModuleRegistration<IModule> moduleRegistration = new(mockRouterBuilder.Object, mockHandlerBuilder.Object);
            Assert.That(() => registration(moduleRegistration), Throws.Exception);
        }
    }
}