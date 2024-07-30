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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Solti.Utils.Router
{
    using Internals;
    using Primitives;

    using static Properties.Resources;

    /// <summary>
    /// Builds router delegate (<see cref="AsyncRouter"/>) that dispatches requests to async handlers.
    /// </summary>
    public sealed class AsyncRouterBuilder
    {
        #region Private
        private delegate Task<object?> AsyncExceptionHandler<TException>(object? userData, TException exc) where TException : Exception;

        private AsyncRouterBuilder(RouterBuilder builder) => FUnderlyingBuilder = builder;

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
            FConvertSingleTask = MethodInfoExtractor.Extract(static () => Convert((Task)null!)),
            FConvertTypedTask  = MethodInfoExtractor.Extract(static () => Convert((Task<object>)null!)).GetGenericMethodDefinition(),
            #pragma warning restore CS4014
            FGetType           = MethodInfoExtractor.Extract<object>(static o => o.GetType()),
            FTaskFromResult    = MethodInfoExtractor.Extract(static () => Task.FromResult((object?)null));

        private readonly RouterBuilder FUnderlyingBuilder;

        private readonly List<LambdaExpression> FExceptionHandlers = new();

        private static LambdaExpression Wrap(LambdaExpression sourceDelegate, Type destinationDelegate)
        {
            Type originalReturnType = sourceDelegate.ReturnType;

            ParameterExpression[] paramz = destinationDelegate
                .GetMethod(nameof(Action.Invoke))
                .GetParameters()
                .Select(static p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();

            Expression body = !typeof(Task).IsAssignableFrom(originalReturnType)
                //
                // object DestinationMethod([paramz]) => Task.FromResult<object>(sourceMethod([paramz]));
                //

                ? Expression.Call
                (
                    FTaskFromResult,
                    Expression.Convert  // convert required as we may encounter value types here
                    (
                        UnfoldedLambda.Create(sourceDelegate, paramz),
                        typeof(object)
                    )
                )

                //
                // object DestinationMethod([paramz])
                // {
                //     return (object) Convert(sourceMethod([paramz]));
                //     async Task<object?> Convert(Task<T> task) => await task;
                // }
                //

                : Expression.Call
                (
                    originalReturnType == typeof(Task)
                        ? FConvertSingleTask  // Task
                        : FConvertTypedTask.MakeGenericMethod // Task<T>
                        (
                            originalReturnType.GetGenericArguments().Single()
                        ),
                    UnfoldedLambda.Create(sourceDelegate, paramz)
                );

            LambdaExpression result = Expression.Lambda(destinationDelegate, body, paramz);
            Debug.WriteLine(result.GetDebugView());
            return result;
        }

        private Expression<AsyncExceptionHandler<Exception>> BuildExceptionHandler()
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
                            exceptionHandler =>
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

            return excHandlerExpr;
        }

        private static Expression<TDestinationDelegate> Wrap<TDestinationDelegate>(LambdaExpression sourceDelegate) where TDestinationDelegate : Delegate =>
            (Expression<TDestinationDelegate>) Wrap(sourceDelegate, typeof(TDestinationDelegate));

        private static Type CheckHandler(LambdaExpression handlerExpr, Type expected)
        {
            if (handlerExpr is null)
                throw new ArgumentNullException(nameof(handlerExpr));

            Type delegateType = handlerExpr.GetType().GetGenericArguments().Single();  // Expression<TDelegate> should be the only derived
            if (!delegateType.IsGenericType || delegateType.GetGenericTypeDefinition() != expected)
                throw new ArgumentException(INVALID_HANDLER, nameof(handlerExpr));

            return delegateType;
        }
        #endregion

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="handlerExpr">Delegate to handle unknown routes. You may pass async and sync callbacks as well.</param>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be sued.</param>
        public static AsyncRouterBuilder Create(LambdaExpression handlerExpr, IReadOnlyDictionary<string, ConverterFactory>? converters = null)
        {
            CheckHandler(handlerExpr, typeof(DefaultRequestHandler<>));

            return new AsyncRouterBuilder
            (
                new RouterBuilder
                (
                    Wrap<DefaultRequestHandler>(handlerExpr),
                    converters
                )
            );
        }

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="handlerExpr">Delegate to handle unknown routes. You may pass async and sync callbacks as well.</param>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be sued.</param>
        public static AsyncRouterBuilder Create<T>(Expression<DefaultRequestHandler<T>> handlerExpr, IReadOnlyDictionary<string, ConverterFactory>? converters = null) =>
            Create((LambdaExpression) handlerExpr, converters);

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

            handler: static (_, _) => throw new InvalidOperationException(ROUTE_NOT_REGISTERED),
            converters
        );

        /// <summary>
        /// Converters to be used during parameter resolution.
        /// </summary>
        public IReadOnlyDictionary<string, ConverterFactory> Converters => FUnderlyingBuilder.Converters;

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handlerFactory">Delegate responsible for creating the handler function.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(ParsedRoute route, UntypedRequestHandlerFactory handlerFactory, params string[] methods)
        {
            if (handlerFactory is null)
                throw new ArgumentNullException(nameof(handlerFactory));

            FUnderlyingBuilder.AddRoute
            (
                route ?? throw new ArgumentNullException(nameof(route)),
                (route, shortcuts) =>
                {
                    LambdaExpression handlerExpr = handlerFactory(route, shortcuts);
                    CheckHandler(handlerExpr, typeof(RequestHandler<>));
                    return Wrap<RequestHandler>(handlerExpr);
                },
                methods ?? throw new ArgumentNullException(nameof(methods))
            );
        }

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handlerExpr">Function accepting requests on the given route. You may pass async and sync callbacks as well.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(ParsedRoute route, LambdaExpression handlerExpr, params string[] methods)
        {
            if (handlerExpr is null)
                throw new ArgumentNullException(nameof(handlerExpr));

            AddRoute(route, (_, _) => handlerExpr, methods);
        }

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handlerExpr">Function accepting requests on the given route. You may pass async and sync callbacks as well.</param>
        /// <param name="splitOptions">Specifies how to split the <paramref name="route"/>.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute<T>(string route, Expression<RequestHandler<T>> handlerExpr, SplitOptions splitOptions, params string[] methods) => AddRoute
        (
            RouteTemplate.Parse(route, FUnderlyingBuilder.Converters, splitOptions),
            handlerExpr,
            methods
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
        public void RegisterExceptionHandler(LambdaExpression handlerExpr)
        {
            Type
                exceptionType = CheckHandler(handlerExpr, typeof(ExceptionHandler<,>)).GetGenericArguments()[0],
                concreteHandler = typeof(AsyncExceptionHandler<>).MakeGenericType(exceptionType);

            FExceptionHandlers.Add
            (
                Wrap(handlerExpr, concreteHandler)
            );
        }

        /// <summary>
        /// Registers a new exception handler.
        /// </summary>
        public void RegisterExceptionHandler<TException, T>(Expression<ExceptionHandler<TException, T>> handlerExpr) where TException : Exception =>
            RegisterExceptionHandler((LambdaExpression) handlerExpr);

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
            DelegateCompiler compiler = new();

            FutureDelegate<AsyncExceptionHandler<Exception>>? excHandler = FExceptionHandlers.Count > 0
                ? compiler.Register(BuildExceptionHandler())
                : null;
            FutureDelegate<Router> router = FUnderlyingBuilder.Build(compiler);

            compiler.Compile();

            return AsyncRouter;

            // methods having ref struct parameter cannot be async =(
            Task<object?> AsyncRouter(object? userData, ReadOnlySpan<char> path, ReadOnlySpan<char> method, SplitOptions? splitOptions)
            {
                Task<object?> task;
                try
                {
                    task = (Task<object?>) router.Value(userData, path, method, splitOptions)!;      
                }
                catch(Exception ex)
                {
                    task = Task.FromException<object?>(ex);
                }

                // CS4012 workaround
                return CallAsync(task, userData);
            };

            async Task<object?> CallAsync(Task<object?> task, object? userData)
            {
                try
                {
                    return await task;
                }
                catch (Exception ex) when (excHandler is not null)
                {
                    return await excHandler.Value(userData, ex);
                }
            }
        }
    }
}
