/********************************************************************************
* AsyncRouterBuilder.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Solti.Utils.Router
{
    using Primitives;
    using Properties;

    /// <summary>
    /// Builds router delegate (<see cref="RouterAsync"/>) that dispatches requests to async handlers.
    /// </summary>
    public sealed class AsyncRouterBuilder
    {
        private AsyncRouterBuilder(RouterBuilder builder) => UnderlyingBuilder = builder;

        private static async Task<object?> Wrap(Task result)
        {
            await result;
            return null;
        }

        private static async Task<object?> Wrap<T>(Task<T> result) => await result;

        private static readonly MethodInfo
#pragma warning disable CS4014
            FWrapSingleTask  = MethodInfoExtractor.Extract(static () => Wrap((Task) null!)), 
            FWrapTypedTask   = MethodInfoExtractor.Extract(static () => Wrap((Task<object>) null!)).GetGenericMethodDefinition();
#pragma warning restore CS4014

        private static Expression<TDestinationDelegate> Wrap<TDestinationDelegate>(LambdaExpression sourceDelegate) where TDestinationDelegate: Delegate
        {           
            Type originaReturnType = sourceDelegate.ReturnType;
            
            MethodInfo wrapped = typeof(TDestinationDelegate).GetMethod(nameof(Action.Invoke));

            ParameterExpression[] paramz = wrapped
                .GetParameters()
                .Select(static p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();

            Expression body;

            if (!typeof(Task).IsAssignableFrom(originaReturnType))
            {
                body = Expression.Convert
                (
                    Expression.Invoke(sourceDelegate, paramz),
                    wrapped.ReturnType
                );
            }
            else
            {
                body = originaReturnType == typeof(Task)
                    ? CreateWrap(FWrapSingleTask)  // Task
                    : CreateWrap  // Task<T>
                    (
                        FWrapTypedTask.MakeGenericMethod
                        (
                            originaReturnType.GetGenericArguments().Single()
                        )
                    );

                Expression CreateWrap(MethodInfo wrap) => Expression.Block
                (
                    type: wrapped.ReturnType,
                    Expression.Convert
                    (
                        Expression.Call
                        (
                            wrap,
                            Expression.Invoke(sourceDelegate, paramz)
                        ),
                        wrapped.ReturnType
                    )
                );
            }

            Expression<TDestinationDelegate> result = Expression.Lambda<TDestinationDelegate>(body, paramz);
            Debug.WriteLine(result.GetDebugView());
            return result;
        }

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="handlerExpr">Delegate to handle unknown routes.</param>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be sued.</param>
        public static AsyncRouterBuilder Create<T>(Expression<DefaultRequestHandler<T>> handlerExpr, IReadOnlyDictionary<string, ConverterFactory>? converters = null) => new
        (
            new RouterBuilder
            (
                Wrap<DefaultRequestHandler>(handlerExpr ?? throw new ArgumentNullException(nameof(handlerExpr))),
                converters
            )
        );

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="handler">Delegate to handle unknown routes.</param>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be sued.</param>
        public static AsyncRouterBuilder Create<T>(DefaultRequestHandler<T> handler, IReadOnlyDictionary<string, ConverterFactory>? converters = null) => Create<T>
        (
            handlerExpr: handler is not null
                ? (state, reason) => handler(state, reason)
                : throw new ArgumentNullException(nameof(handler)),
            converters
        );

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be used.</param>
        public static AsyncRouterBuilder Create(IReadOnlyDictionary<string, ConverterFactory>? converters = null) => Create<object?>
        (
            //
            // Compiler generated expression tree cannot contain throw expression (CS8188)
            //

            handler: static (_, _) => throw new InvalidOperationException(Resources.ROUTE_NOT_REGISTERED),
            converters
        );

        /// <summary>
        /// The utilized <see cref="RouterBuilder"/> instance.
        /// </summary>
        public RouterBuilder UnderlyingBuilder { get; }

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handlerExpr">Function accepting requests on the given route.</param>
        /// <param name="splitOptions">Specifies how to split the <paramref name="route"/>.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute<T>(string route, Expression<RequestHandler<T>> handlerExpr, SplitOptions splitOptions, params string[] methods) => UnderlyingBuilder.AddRoute
        (
            route ?? throw new ArgumentNullException(nameof(route)),
            Wrap<RequestHandler>(handlerExpr ?? throw new ArgumentNullException(nameof(handlerExpr))),
            splitOptions ?? throw new ArgumentNullException(nameof(splitOptions)),
            methods ?? throw new ArgumentNullException(nameof(methods))
        );

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handlerExpr">Function accepting requests on the given route.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute<T>(string route, Expression<RequestHandler<T>> handlerExpr, params string[] methods) =>
            AddRoute(route, handlerExpr, SplitOptions.Default, methods);

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handler">Function accepting requests on the given route.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute<T>(string route, RequestHandler<T> handler, params string[] methods) =>
            AddRoute(route, handler, SplitOptions.Default, methods);

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handler">Function accepting requests on the given route.</param>
        /// <param name="splitOptions">Specifies how to split the <paramref name="route"/>.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute<T>(string route, RequestHandler<T> handler, SplitOptions splitOptions, params string[] methods) => AddRoute<T>
        (
            route,
            handlerExpr: handler is not null
                ? (paramz, state) => handler(paramz, state)
                : throw new ArgumentNullException(nameof(handler)),
            splitOptions,
            methods
        );

        /// <summary>
        /// Builds the actual <see cref="RouterAsync"/> delegate.
        /// </summary>
        public RouterAsync Build()
        {
            Router router = UnderlyingBuilder.Build();

            return (object? userData, string path, string method, SplitOptions? splitOptions) =>
            {
                object? result = router(userData, path, method, splitOptions);

                return result is Task<object?> task
                    ? task
                    : Task.FromResult(result);
            };
        }
    }
}
