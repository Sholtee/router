/********************************************************************************
* ModuleRegistration.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Router.Extensions
{
    using Properties;

    /// <summary>
    /// Encapsulates route registratons that are specific for a given module.
    /// </summary>
    public class ModuleRegistration<TModule>(IRouterBuilder routerBuilder, RequestHandlerBuilder handlerBuilder)
    {
        private readonly IRouterBuilder FRouterBuilder = routerBuilder ?? throw new ArgumentNullException(nameof(routerBuilder));

        private readonly RequestHandlerBuilder FHandlerBuilder = handlerBuilder ?? throw new ArgumentNullException(nameof(handlerBuilder));

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        protected ModuleRegistration<TModule> AddRouteInternal<TDelegate>(ParsedRoute route, Expression<Func<TModule, TDelegate>> selector, string[] methods) where TDelegate : Delegate
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            if (selector.Body is not UnaryExpression unary || unary.Operand is not MethodCallExpression convert || convert.Object is not ConstantExpression constant || constant.Value is not MethodInfo m)
                throw new ArgumentException(Resources.INVALID_SELECTOR, nameof(selector));

            FRouterBuilder.AddRoute
            (
                route,
                FHandlerBuilder.CreateFactory(route, m, null),
                methods
            );

            return this;
        }

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        protected ModuleRegistration<TModule> AddRouteInternal<TDelegate>(string route, Expression<Func<TModule, TDelegate>> selector, string[] methods) where TDelegate : Delegate =>
            AddRouteInternal(RouteTemplate.Parse(route), selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute(string route, Expression<Func<TModule, Action>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T>(string route, Expression<Func<TModule, Action<T>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T1, T2>(string route, Expression<Func<TModule, Action<T1, T2>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T1, T2, T3>(string route, Expression<Func<TModule, Action<T1, T2, T3>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T1, T2, T3, T4>(string route, Expression<Func<TModule, Action<T1, T2, T3, T4>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T1, T2, T3, T4, T5>(string route, Expression<Func<TModule, Action<T1, T2, T3, T4, T5>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<TResult>(string route, Expression<Func<TModule, Func<TResult>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T, TResult>(string route, Expression<Func<TModule, Func<T, TResult>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T1, T2, TResult>(string route, Expression<Func<TModule, Func<T1, T2, TResult>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T1, T2, T3, TResult>(string route, Expression<Func<TModule, Func<T1, T2, T3, TResult>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T1, T2, T3, T4, TResult>(string route, Expression<Func<TModule, Func<T1, T2, T3, T4, TResult>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);

        /// <summary>
        /// Registers a module method (specified by the <paramref name="selector"/> expression) to handle incoming requests on the given <paramref name="route"/>.
        /// </summary>
        public ModuleRegistration<TModule> AddRoute<T1, T2, T3, T4, T5, TResult>(string route, Expression<Func<TModule, Func<T1, T2, T3, T4, T5, TResult>>> selector, params string[] methods) =>
            AddRouteInternal(route, selector, methods);
    }
}
