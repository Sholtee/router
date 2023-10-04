﻿/********************************************************************************
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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Solti.Utils.Router
{
    using Internals;
    using Primitives;
    using Properties;

    /// <summary>
    /// Builds router delegate (<see cref="AsyncRouter"/>) that dispatches requests to async handlers.
    /// </summary>
    public sealed class AsyncRouterBuilder
    {
        private delegate Task<object?> AsyncExceptionHandler<TException>(object? userData, TException exc) where TException : Exception;

        private AsyncRouterBuilder(RouterBuilder builder) => UnderlyingBuilder = builder;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<object?> Convert(Task result)
        {
            await result;
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<object?> Convert<T>(Task<T> result) => await result;

        private static readonly MethodInfo
#pragma warning disable CS4014
            FConvertSingleTask = MethodInfoExtractor.Extract(static () => Convert((Task) null!)), 
            FConvertTypedTask  = MethodInfoExtractor.Extract(static () => Convert((Task<object>) null!)).GetGenericMethodDefinition(),
#pragma warning restore CS4014
            FGetType           = MethodInfoExtractor.Extract<object>(static o => o.GetType()),
            FTaskFromResult    = MethodInfoExtractor.Extract(static () => Task.FromResult((object?) null));

        private readonly IList<LambdaExpression> FExceptionHandlers = new List<LambdaExpression>();

        private static Expression<TDestinationDelegate> Wrap<TDestinationDelegate>(LambdaExpression sourceDelegate) where TDestinationDelegate : Delegate
        {
            Type originalReturnType = sourceDelegate.ReturnType;
            
            MethodInfo detinationMethod = typeof(TDestinationDelegate).GetMethod(nameof(Action.Invoke));

            ParameterExpression[] paramz = detinationMethod
                .GetParameters()
                .Select(static p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();

            Expression body;

            if (!typeof(Task).IsAssignableFrom(originalReturnType))
            {
                //
                // object DestinationMethod([paramz]) => Task.FromResult<object>(sourceMethod([paramz]));
                //

                body = Expression.Convert
                (
                    Expression.Call
                    (
                        FTaskFromResult,
                        Expression.Convert
                        (
                            UnfoldedLambda.Create(sourceDelegate, paramz),
                            typeof(object)
                        )
                    ),
                    detinationMethod.ReturnType // typeof(object)
                );
            }
            else
            {
                //
                // object DestinationMethod([paramz])
                // {
                //     return (object) Convert(sourceMethod([paramz]));
                //     async Task<object?> Convert(Task<T> task) => await task;
                // }
                //

                body = originalReturnType == typeof(Task)
                    ? WrapUsing(FConvertSingleTask)  // Task
                    : WrapUsing  // Task<T>
                    (
                        FConvertTypedTask.MakeGenericMethod
                        (
                            originalReturnType.GetGenericArguments().Single()
                        )
                    );

                Expression WrapUsing(MethodInfo taskConverter) => Expression.Block
                (
                    type: detinationMethod.ReturnType,
                    Expression.Convert
                    (
                        Expression.Call
                        (
                            taskConverter,
                            UnfoldedLambda.Create(sourceDelegate, paramz)
                        ),
                        detinationMethod.ReturnType
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
        /// <param name="handlerExpr">Delegate to handle unknown routes. You may pass async and sync callbacks as well.</param>
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
        /// <param name="handler">Delegate to handle unknown routes. You may pass async and sync callbacks as well.</param>
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
        /// <param name="handlerExpr">Function accepting requests on the given route. You may pass async and sync callbacks as well.</param>
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
        /// <param name="handlerExpr">Function accepting requests on the given route. You may pass async and sync callbacks as well.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute<T>(string route, Expression<RequestHandler<T>> handlerExpr, params string[] methods) =>
            AddRoute(route, handlerExpr, SplitOptions.Default, methods);

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handler">Function accepting requests on the given route. You may pass async and sync callbacks as well.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute<T>(string route, RequestHandler<T> handler, params string[] methods) =>
            AddRoute(route, handler, SplitOptions.Default, methods);

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handler">Function accepting requests on the given route. You may pass async and sync callbacks as well.</param>
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
        /// Registers a new exception handler.
        /// </summary>
        public void RegisterExceptionHandler<TException, T>(Expression<ExceptionHandler<TException, T>> handlerExpr) where TException : Exception => FExceptionHandlers.Add
        (
            Wrap<AsyncExceptionHandler<TException>>(handlerExpr ?? throw new ArgumentNullException(nameof(handlerExpr)))
        );

        /// <summary>
        /// Registers a new exception handler.
        /// </summary>
        public void RegisterExceptionHandler<TException, T>(ExceptionHandler<TException, T> handler) where TException : Exception => RegisterExceptionHandler<TException, T>
        (
            handlerExpr: handler is not null
                ? (userData, exc) => handler(userData, exc)
                : throw new ArgumentNullException(nameof(handler))
        );

        /// <summary>
        /// Builds the actual <see cref="AsyncRouter"/> delegate.
        /// </summary>
        public AsyncRouter Build()
        {
            AsyncExceptionHandler<Exception>? excHandler = null;
            if (FExceptionHandlers.Count > 0)
            {
                ParameterExpression
                    userData = Expression.Parameter(typeof(object), nameof(userData)),
                    exc = Expression.Parameter(typeof(Exception), nameof(exc));

                LabelTarget exit = Expression.Label(typeof(Task<object?>), nameof(exit));

                Expression<AsyncExceptionHandler<Exception>> excHandlerExpr = Expression.Lambda<AsyncExceptionHandler<Exception>>
                (
                    Expression.Block
                    (
                        Expression.Switch
                        (
                            Expression.Call(exc, FGetType),
                            FExceptionHandlers.Select
                            (
                                (LambdaExpression exceptionHandler) =>
                                {
                                    Type excType = exceptionHandler.Parameters.Last().Type;
                                    Debug.Assert(typeof(Exception).IsAssignableFrom(excType), "Not an exception handler");

                                    return Expression.SwitchCase
                                    (
                                        Expression.Return
                                        (
                                            exit,
                                            Expression.Invoke
                                            (
                                                exceptionHandler,
                                                userData, 
                                                Expression.Convert(exc, excType)
                                            )
                                        ),
                                        Expression.Constant(excType)
                                    );
                                }
                            ).ToArray()
                        ),
                        Expression.Throw(exc),
                        Expression.Label(exit, Expression.Default(typeof(Task<object?>)))
                    ),
                    userData,
                    exc
                );

                Debug.WriteLine(excHandlerExpr.GetDebugView());
                excHandler = excHandlerExpr.Compile();
            }

            Router router = UnderlyingBuilder.Build();

            return AsyncRouter;

            async Task<object?> AsyncRouter(object? userData, string path, string method, SplitOptions? splitOptions)
            {
                try
                {
                    Task<object?> result = (Task<object?>) router(userData, path, method, splitOptions)!;
                    return await result;
                }
                catch(Exception ex) when (excHandler is not null)
                {
                    return await excHandler(userData, ex);
                } 
            };
        }
    }
}
