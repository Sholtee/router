/********************************************************************************
* AsyncRouterBuilderAddRouteExtensions.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.Router.Extensions
{
    /// <summary>
    /// Defines some extension methods on <see cref="AsyncRouterBuilder"/>.
    /// </summary>
    public static class AsyncRouterBuilderAddRouteExtensions
    {
        /// <summary>
        /// The <see cref="Extensions.RequestHandlerBuilder"/> to be used. The default is <see cref="MsDiRequestHandlerBuilder"/>
        /// </summary>
        public static RequestHandlerBuilder RequestHandlerBuilder { get; set; } = new MsDiRequestHandlerBuilder();

        /// <summary>
        /// Begins a registration block where each route is bound to a specific module.
        /// </summary>
        public static ModuleRegistration<TModule> UseModule<TModule>(this AsyncRouterBuilder self) => new(self, RequestHandlerBuilder);

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="self"><see cref="AsyncRouterBuilder"/> instance.</param>
        /// <param name="route">Route to be registered.</param>
        /// <param name="handler">Method accepting requests on the given route. You may pass async and sync callbacks as well.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public static void AddRoute(this AsyncRouterBuilder self, ParsedRoute route, MethodInfo handler, params string[] methods)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            self.AddRoute
            (
                route,
                RequestHandlerBuilder.CreateFactory(route, handler, null),
                methods
            );
        }

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="self"><see cref="AsyncRouterBuilder"/> instance.</param>
        /// <param name="route">Route to be registered.</param>
        /// <param name="handler">Method accepting requests on the given route. You may pass async and sync callbacks as well.</param>
        /// <param name="splitOptions">Specifies how to split the <paramref name="route"/>.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public static void AddRoute(this AsyncRouterBuilder self, string route, MethodInfo handler, SplitOptions splitOptions, params string[] methods) => self.AddRoute
        (
            RouteTemplate.Parse(route, self.Converters, splitOptions),
            handler,
            methods
        );

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="self"><see cref="AsyncRouterBuilder"/> instance.</param>
        /// <param name="route">Route to be registered.</param>
        /// <param name="handler">Method accepting requests on the given route. You may pass async and sync callbacks as well.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public static void AddRoute(this AsyncRouterBuilder self, string route, MethodInfo handler, params string[] methods) => self.AddRoute
        (
            route,
            handler,
            SplitOptions.Default,
            methods
        );
    }
}
