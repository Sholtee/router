/********************************************************************************
* RouterBuilder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router
{
    using Internals;
    using Primitives;

    using static Properties.Resources;

    /// <summary>
    /// Builds the <see cref="Router"/> delegate that does the actual routing.
    /// <code>
    /// object Route(object? userData, ReadOnlySpan&lt;char&gt; path, ReadOnlySpan&lt;char&gt; method, SplitOptions? splitOptions = null)
    /// { 
    ///     [try]
    ///     {
    ///         using PathSplitter segments = PathSplitter.Split(path, splitOptions);
    ///         StaticDictionary&lt;object?&gt; paramz = createParamzDict();
    ///         object converted;
    ///    
    ///         if (segments.MoveNext())
    ///         {
    ///             if (segments.Current == "cica")
    ///             {
    ///                 if (segments.MoveNext())
    ///                 {
    ///                     if (segments.Current == "mica")
    ///                     {
    ///                         if (segments.MoveNext())
    ///                         {
    ///                             return DefaultHandler(userData, HttpStatusCode.NotFound);
    ///                         }
    /// 
    ///                         if (method == "GET")
    ///                         {
    ///                             return CicaMicaHandler(paramz, userData); // GET "/cica/mica" defined
    ///                         }
    ///                     
    ///                         return DefaultHandler(userData, HttpStatusCode.MethodNotAllowed);  // Unknown method
    ///                     }
    ///                     
    ///                     if (intParser(segments.Current, out converted))
    ///                     {
    ///                         paramz.Add("id", val);
    ///                         
    ///                         if (segments.MoveNext())
    ///                         {
    ///                             return DefaultHandler(userData, HttpStatusCode.NotFound);
    ///                         }
    /// 
    ///                         if (method == "GET" || method == "POST" )
    ///                         {
    ///                             return CicaIdHandler(paramz, userData); // GET | POST "/cica/{id:int}" defined
    ///                         }
    ///                     
    ///                         return DefaultHandler(userData, HttpStatusCode.MethodNotAllowed);
    ///                     }
    ///                     
    ///                     return DefaultHandler(userData, HttpStatusCode.NotFound);  // neither "/cica/mica" nor "/cica/{id:int}"
    ///                 }
    ///             
    ///                 if (method == "GET")
    ///                 {
    ///                     return CicaHandler(paramz, userData); // GET "/cica" defined
    ///                 }
    ///             
    ///                 return DefaultHandler(userData, HttpStatusCode.MethodNotAllowed);
    ///             }
    ///             
    ///             return DefaultHandler(userData, HttpStatusCode.NotFound);  // not "/cica[/..]"
    ///         }
    ///         
    ///         if (method == "GET")
    ///         {
    ///             return RootHandler(paramz, userData);  // GET "/" is defined
    ///         }
    ///     
    ///         return DefaultHandler(userData, HttpStatusCode.MethodNotAllowed);
    ///     } 
    ///     [catch(Exception exc) { return ExceptionHandler(userData, exc); }]
    /// </code>
    /// </summary>
    public sealed class RouterBuilder: IRouterBuilder
    {
        #region Private
        private delegate bool MemoryEqualsDelegate(ReadOnlySpan<char> left, ReadOnlySpan<char> right, StringComparison comparison);

        private delegate ReadOnlySpan<char> AsSpanDelegate(string s);

        private delegate PathSplitter SplitDelegate(ReadOnlySpan<char> path, SplitOptions? options);

        private sealed class Junction
        {
            public RouteSegment? Segment { get; init; }  // null at "/"
            // order doesn't matter
            public Dictionary<string, Expression<RequestHandler>> Methods { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<Junction> Children { get; } = [];
        }

        private static readonly MethodInfo
            FMoveNext     = typeof(PathSplitter).GetMethod(nameof(PathSplitter.MoveNext)), // MethodInfoExtractor.Extract<PathSplitter>(static ps => ps.MoveNext()),
            FDispose      = typeof(PathSplitter).GetMethod(nameof(PathSplitter.Dispose)), // MethodInfoExtractor.Extract<PathSplitter>(static ps => ps.Dispose()),
            FSplit        = ((SplitDelegate) Split).Method, // MethodInfoExtractor.Extract(static () => Split(default, SplitOptions.Default)),
            FAddParam     = MethodInfoExtractor.Extract(static () => AddParam(null!, 0, null)),
            FMemoryEquals = ((MemoryEqualsDelegate) System.MemoryExtensions.Equals).Method, // MethodInfoExtractor.Extract(static () => MemoryEquals(default, string.Empty, default)),
            FAsSpan       = ((AsSpanDelegate) System.MemoryExtensions.AsSpan).Method,
            FConvert      = MethodInfoExtractor.Extract<IConverter, object?>(static (c, output) => c.ConvertToValue(null!, out output));

        private static readonly PropertyInfo FCurrent = typeof(PathSplitter).GetProperty(nameof(PathSplitter.Current)); // PropertyInfoExtractor.Extract<PathSplitter, ReadOnlySpan<char>>(static parts => parts.Current);

        private static readonly ParameterExpression
            FUserData     = Expression.Parameter(typeof(object), "userData"),
            FPath         = Expression.Parameter(typeof(ReadOnlySpan<char>), "path"),
            FMethod       = Expression.Parameter(typeof(ReadOnlySpan<char>), "method"),
            FSplitOptions = Expression.Parameter(typeof(SplitOptions), "splitOptions"),

            FSegments  = Expression.Variable(typeof(PathSplitter), "segments"),
            FParams    = Expression.Variable(typeof(StaticDictionary<object?>), "params"),
            FConverted = Expression.Variable(typeof(object), "converted"),
            FOrder     = Expression.Variable(typeof(int), "order");

        private static  readonly LabelTarget FExit = Expression.Label(typeof(object), "exit");

        private readonly Junction FRoot = new();

        private readonly StaticDictionary<object?>.Builder FParameters = new();

        private readonly List<LambdaExpression> FExceptionHandlers = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddParam(StaticDictionary<object?> paramz, int id, object? value) => paramz[id] = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PathSplitter Split(ReadOnlySpan<char> path, SplitOptions? options) => new(path, options);

        private Expression Build(IReadOnlyDictionary<string, int> shortcuts)
        {
            ApplyShortcuts applyShortcuts = new(shortcuts);

            return Expression.Block
            (
                ProcessJunction(FRoot)
            );

            IEnumerable<Expression> ProcessStaticJunctions(IEnumerable<Junction> junctions)
            {
                if (!junctions.Any())
                    yield break;

                LabelTarget exit = Expression.Label(nameof(exit));

                SwitchExpression @switch = new
                (
                    ignoreCase: true,
                    order: FOrder,
                    key: Expression.Property(FSegments, FCurrent),
                    @default: Expression.Goto(exit)
                );

                foreach (Junction junction in junctions)
                {
                    @switch.AddCase
                    (
                        junction.Segment!.Name,
                        Expression.Block
                        (
                            ProcessJunction(junction)
                        )
                    );
                }

                yield return @switch.Expression;

                yield return Expression.Label(exit);
            }

            IEnumerable<Expression> ProcessDynamicJunctions(IEnumerable<Junction> junctions)
            {
                foreach (Junction junction in junctions)
                {
                    yield return Expression.IfThen
                    (
                        Expression.Call
                        (
                            Expression.Constant(junction.Segment!.Converter),
                            FConvert,
                            Expression.Property(FSegments, FCurrent),
                            FConverted
                        ),
                        Expression.Block
                        (
                            new Expression[]
                            {
                                Expression.Call
                                (
                                    FAddParam,
                                    FParams,
                                    Expression.Constant(shortcuts[junction.Segment.Name]),
                                    FConverted
                                )
                            }.Concat
                            (
                                ProcessJunction(junction)
                            )
                        )
                    );
                }
            }

            IEnumerable<Expression> ProcessJunction(Junction junction)
            {
                //
                // if (segments.MoveNext())
                // {
                //     ...;
                //     return DefaultHandler(state, HttpStatusCode.NotFound);
                // }
                //

                yield return Expression.IfThen
                (
                    Expression.Call(FSegments, FMoveNext),
                    Expression.Block
                    ([
                        ..ProcessStaticJunctions(junction.Children.Where(static child => child.Segment!.Converter is null)),
                        ..ProcessDynamicJunctions(junction.Children.Where(static child => child.Segment!.Converter is not null)),
                        Return
                        (
                            DefaultHandler,
                            FUserData,
                            Expression.Constant(HttpStatusCode.NotFound)
                        )
                    ])
                );

                //
                // if (method == "POST" || method == "GET")
                //    return AbcHandler(...);
                //
                // if (method == "OPTIONS")
                //    return XyzHandler(...);
                //

                foreach (IGrouping<Expression<RequestHandler>, string> handlerGroup in junction.Methods.GroupBy(static fact => fact.Value, static fact => fact.Key))
                {
                    yield return Expression.IfThen
                    (
                        handlerGroup
                            .Select(static supportedMethod => Equals(FMethod, supportedMethod))
                            .Aggregate(static (accu, curr) => Expression.Or(accu, curr)),
                        applyShortcuts.Visit
                        (
                            Return
                            (
                                handlerGroup.Key,
                                FParams,
                                FUserData
                            )
                        )
                    );
                }

                //
                // return DefaultHandler(state, HttpStatusCode.[xXx]);
                //

                yield return Return
                (
                    DefaultHandler,
                    FUserData,
                    Expression.Constant
                    (
                        junction.Methods.Count is 0
                            ? HttpStatusCode.NotFound
                            : HttpStatusCode.MethodNotAllowed
                    )
                );
            }

            static Expression Equals(Expression left, string right) => Expression.Call
            (
                FMemoryEquals,
                left,
                Expression.Call
                (
                    FAsSpan,
                    Expression.Constant(right)
                ),
                Expression.Constant(StringComparison.OrdinalIgnoreCase)
            );
        }

        private static Expression Return(LambdaExpression lambda, params Expression[] paramz) => Expression.Return
        (
            FExit,
            UnfoldedLambda.Create
            (
                lambda,
                paramz
            ),
            typeof(object)
        );
        #endregion

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="handlerExpr">Delegate to handle unknown routes.</param>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be used.</param>
        public RouterBuilder(Expression<DefaultRequestHandler> handlerExpr, IReadOnlyDictionary<string, ConverterFactory>? converters = null)
        {
            DefaultHandler = handlerExpr ?? throw new ArgumentNullException(nameof(handlerExpr));
            Converters = converters ?? DefaultConverters.Instance;
        }

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="handler">Delegate to handle unknown routes.</param>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be used.</param>
        public RouterBuilder(DefaultRequestHandler handler, IReadOnlyDictionary<string, ConverterFactory>? converters = null) : this
        (
            handlerExpr: handler is not null
                ? (state, reason) => handler(state, reason)
                : throw new ArgumentNullException(nameof(handler)),
            converters
        ) { }

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be used.</param>
        public RouterBuilder(IReadOnlyDictionary<string, ConverterFactory>? converters = null) : this
        (
            //
            // Compiler generated expression tree cannot contain throw expression (CS8188)
            //

            handler: static (_, _) => throw new InvalidOperationException(ROUTE_NOT_REGISTERED),
            converters
        ) { }

        internal FutureDelegate<Router> Build(DelegateCompiler compiler)
        {
            StaticDictionaryFactory<object?> createParamzDict = FParameters.CreateFactory(compiler, out IReadOnlyDictionary<string, int> shortcuts);

            Expression route = Expression.Block
            (
                Expression.Assign
                (
                    FSegments,
                    Expression.Call(FSplit, FPath, FSplitOptions)
                ),
                Expression.TryFinally
                (
                    Expression.Block
                    (
                        Expression.Assign
                        (
                            FParams,
                            Expression.Invoke
                            (
                                Expression.Constant(createParamzDict)
                            )
                        ),
                        Build(shortcuts)
                    ),
                    @finally: Expression.Call(FSegments, FDispose)
                )
            );
            if (FExceptionHandlers.Count > 0) route = Expression.TryCatch
            (
                route,
                FExceptionHandlers.Select
                (
                    static exceptionHandler =>
                    {
                        Type excType = exceptionHandler.Parameters.Last().Type;
                        Debug.Assert(typeof(Exception).IsAssignableFrom(excType), "Not an exception handler");

                        ParameterExpression exception = Expression.Variable(excType, nameof(exception));

                        return Expression.Catch
                        (
                            exception,
                            Return
                            (
                                exceptionHandler,
                                FUserData,
                                exception
                            )
                        );
                    }
                ).ToArray()
            );

            Expression<Router> routerExpr = Expression.Lambda<Router>
            (
                body: Expression.Block
                (
                    variables:
                    [
                        FSegments,
                        FParams,
                        FConverted,
                        FOrder
                    ],
                    FPath,
                    FMethod,
                    route,
                    Expression.Label
                    (
                        FExit,
                        Expression.Invoke
                        (
                            Expression.Constant((Func<object?>) Disaster)
                        )
                    )
                ),
                parameters:
                [
                    FUserData,
                    FPath,
                    FMethod,
                    FSplitOptions
                ]
            );

            Debug.WriteLine(routerExpr.GetDebugView());

            return compiler.Register(routerExpr);

            static object? Disaster()
            {
                Debug.Fail("The code should have never reached here");
                return null;
            }
        }

        /// <summary>
        /// Converters to be used during parameter resolution.
        /// </summary>
        public IReadOnlyDictionary<string, ConverterFactory> Converters { get; }

        /// <summary>
        /// Builds the actual <see cref="Router"/> delegate.
        /// </summary>
        public Router Build()
        {
            DelegateCompiler compiler = new();
            FutureDelegate<Router> router = Build(compiler);
            compiler.Compile();
            return router.Value;
        }

        /// <summary>
        /// Delegate that handles the unknown routes.
        /// </summary>
        public Expression<DefaultRequestHandler> DefaultHandler { get; }

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered.</param>
        /// <param name="handlerExpr">Function accepting requests on the given route.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(ParsedRoute route, Expression<RequestHandler> handlerExpr, params string[] methods)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            if (handlerExpr is null)
                throw new ArgumentNullException(nameof(handlerExpr));

            if (methods is null)
                throw new ArgumentNullException(nameof(methods));

            Junction target = FRoot;

            foreach (RouteSegment segment in route.Segments)
            {
                bool found = false;

                foreach (Junction child in target.Children)
                {
                    Debug.Assert(child.Segment is not null, "Root cannot be a child");

                    if
                    (
                        (segment.Converter is null && segment.Name.Equals(child.Segment!.Name, StringComparison.OrdinalIgnoreCase)) || 
                        (segment.Converter is not null && segment.Converter.Id == child.Segment!.Converter?.Id)
                    )
                    {
                        target = child;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Junction child = new() { Segment = segment };
                    target.Children.Add(child);
                    target = child;
                }
            }

            if (methods.Length is 0)
                methods = ["GET"];

            foreach (string method in methods)
            {
                if (string.IsNullOrEmpty(method))
                    throw new ArgumentException(EMPTY_METHOD, nameof(methods));

                if (target.Methods.ContainsKey(method))
                    throw new ArgumentException(string.Format(Culture, ROUTE_ALREADY_REGISTERED, route), nameof(route));

                target.Methods[method] = handlerExpr;          
            }

            foreach (string variable in route.Parameters.Keys)
            {
                FParameters.RegisterKey(variable);
            }
        }

        void IRouterBuilder.AddRoute(ParsedRoute route, LambdaExpression handlerExpr, params string[] methods)
        {
            if (handlerExpr is not Expression<RequestHandler> typedHandlerExpr)
                throw new ArgumentException(INVALID_HANDLER, nameof(handlerExpr));

            AddRoute(route, typedHandlerExpr, methods);
        }

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handlerExpr">Function accepting requests on the given route.</param>
        /// <param name="splitOptions">Specifies how to split the <paramref name="route"/>.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(string route, Expression<RequestHandler> handlerExpr, SplitOptions splitOptions, params string[] methods) => AddRoute
        (
            RouteTemplate.Parse
            (
                route ?? throw new ArgumentNullException(nameof(route)),
                Converters,
                splitOptions
            ),
            handlerExpr,
            methods
        );

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handlerExpr">Function accepting requests on the given route.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(string route, Expression<RequestHandler> handlerExpr, params string[] methods) =>
            AddRoute(route, handlerExpr, SplitOptions.Default, methods);

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handler">Function accepting requests on the given route.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(string route, RequestHandler handler, params string[] methods) =>
            AddRoute(route, handler, SplitOptions.Default, methods);

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handler">Function accepting requests on the given route.</param>
        /// <param name="splitOptions">Specifies how to split the <paramref name="route"/>.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(string route, RequestHandler handler, SplitOptions splitOptions, params string[] methods) => AddRoute
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
        public void RegisterExceptionHandler<TException>(Expression<ExceptionHandler<TException>> handlerExpr) where TException : Exception =>
            FExceptionHandlers.Add(handlerExpr ?? throw new ArgumentNullException(nameof(handlerExpr)));

        /// <summary>
        /// Registers a new exception handler.
        /// </summary>
        public void RegisterExceptionHandler<TException>(ExceptionHandler<TException> handler) where TException : Exception => RegisterExceptionHandler<TException>
        (
            handlerExpr: handler is not null
                ? (userData, exc) => handler(userData, exc)
                : throw new ArgumentNullException(nameof(handler))
        );
    }
}
