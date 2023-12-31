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

namespace Solti.Utils.Router
{
    using Internals;
    using Primitives;
    using Properties;

    /// <summary>
    /// Builds the <see cref="Router"/> delegate that does the actual routing.
    /// <code>
    /// object Route(object? userData, string path, string method = "GET", SplitOptions? splitOptions = null)
    /// { 
    ///     [try]
    ///     {
    ///         PathSplitter segments = PathSplitter.Split(path, splitOptions);
    ///         StaticDictionary paramz = createParamzDict();
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
    public sealed class RouterBuilder
    {
        #region Private
        private sealed class Junction
        {
            public RouteSegment? Segment { get; init; }  // null at "/"
            // order doesn't matter
            public Dictionary<string, Expression<RequestHandler>> Handlers { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<Junction> Children { get; } = new();
        }

        private static readonly MethodInfo
            FMoveNext = MethodInfoExtractor.Extract<PathSplitter>(static parts => parts.MoveNext()),
            FSplit    = MethodInfoExtractor.Extract(static () => PathSplitter.Split(null!, SplitOptions.Default)),
            FAddParam = MethodInfoExtractor.Extract<StaticDictionary>(static dict => dict.Add(null!, null)),
            FEquals   = MethodInfoExtractor.Extract<string>(static s => s.Equals(null!, default(StringComparison))),
            FConvert  = MethodInfoExtractor.Extract<IConverter, object?>(static (c, output) => c.ConvertToValue(null!, out output));

        private static readonly PropertyInfo
            FCurrent = PropertyInfoExtractor.Extract<PathSplitter, string>(static parts => parts.Current);

        private readonly Junction FRoot = new();

        private readonly StaticDictionaryBuilder FParameters = new();

        private readonly List<LambdaExpression> FExceptionHandlers = new();

        private readonly ParameterExpression
            FUserData     = Expression.Parameter(typeof(object), "userData"),
            FPath         = Expression.Parameter(typeof(string), "path"),
            FMethod       = Expression.Parameter(typeof(string), "method"),
            FSplitOptions = Expression.Parameter(typeof(SplitOptions), "splitOptions"),

            FSegments  = Expression.Variable(typeof(PathSplitter), "segments"),
            FParams    = Expression.Variable(typeof(StaticDictionary), "params"),
            FConverted = Expression.Variable(typeof(object), "converted");

        private readonly LabelTarget FExit = Expression.Label(typeof(object), "exit");

        private Expression BuildJunction(Junction junction)
        {
            if (junction.Segment is null)  // root node, no segment
                return Expression.Block
                (
                    ProcessJunction()
                );

            if (junction.Segment.Converter is null)
                return Expression.IfThen
                (
                    Equals
                    (
                        Expression.Property(FSegments, FCurrent),
                        Expression.Constant(junction.Segment.Name)
                    ),
                    Expression.Block
                    (
                        ProcessJunction()
                    )
                );

            return Expression.IfThen
            (
                Expression.Call
                (
                    Expression.Constant(junction.Segment.Converter),
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
                            FParams,
                            FAddParam,
                            Expression.Constant(junction.Segment.Name),
                            FConverted
                        )
                    }.Concat
                    (
                        ProcessJunction()
                    )
                )
            );

            IEnumerable<Expression> ProcessJunction()
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
                    (
                        junction
                            .Children
                            .Select(BuildJunction)
                            .Append
                            (
                                Return
                                (
                                    DefaultHandler,
                                    FUserData,
                                    Expression.Constant(HttpStatusCode.NotFound)
                                )
                            )
                    )
                );

                //
                // if (method == "POST" || method == "GET")
                //    return AbcHandler(...);
                //
                // if (method == "OPTIONS")
                //    return XyzHandler(...);
                //

                foreach (IGrouping<Expression<RequestHandler>, KeyValuePair<string, Expression<RequestHandler>>> handlerGroup in junction.Handlers.GroupBy(static handler => handler.Value))
                {
                    yield return Expression.IfThen
                    (
                        handlerGroup
                            .Select(handler => Equals(FMethod, Expression.Constant(handler.Key)))
                            .Aggregate(static (accu, curr) => Expression.Or(accu, curr)),
                        Return
                        (
                            handlerGroup.Key,
                            FParams,
                            FUserData
                        )
                    );
                }

                //
                // return DEfaultHandler(state, HttpStatusCode.[xXx]);
                //

                yield return Return
                (
                    DefaultHandler,
                    FUserData,
                    Expression.Constant
                    (
                        junction.Handlers.Count is 0
                            ? HttpStatusCode.NotFound
                            : HttpStatusCode.MethodNotAllowed
                    )
                );
            }

            static Expression Equals(Expression left, Expression right) => Expression.Call
            (
                left,
                FEquals,
                right,
                Expression.Constant(StringComparison.OrdinalIgnoreCase)
            );

        }

        private Expression Return(LambdaExpression lambda, params Expression[] paramz) => Expression.Return
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

            handler: static (_, _) => throw new InvalidOperationException(Resources.ROUTE_NOT_REGISTERED),
            converters
        ) { }

        internal FutureDelegate<Router> Build(DelegateCompiler compiler)
        {
            StaticDictionaryFactory createParamzDict = FParameters.CreateFactory(compiler);

            Expression route = Expression.Block
            (
                Expression.Assign
                (
                    FSegments,
                    Expression.Call(FSplit, FPath, FSplitOptions)
                ),
                Expression.Assign
                (
                    FParams,
                    Expression.Invoke
                    (
                        Expression.Constant(createParamzDict)
                    )
                ),
                BuildJunction(FRoot)
            );
            if (FExceptionHandlers.Count > 0) route = Expression.TryCatch
            (
                route,
                FExceptionHandlers.Select
                (
                    (LambdaExpression exceptionHandler) =>
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
                    variables: new ParameterExpression[]
                    {
                        FSegments,
                        FParams,
                        FConverted
                    },
                    EnsureNotNull(FPath),
                    EnsureNotNull(FMethod),
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
                parameters: new ParameterExpression[]
                {
                    FUserData,
                    FPath,
                    FMethod,
                    FSplitOptions
                }
            );

            Debug.WriteLine(routerExpr.GetDebugView());

            return compiler.Register(routerExpr);

            static Expression EnsureNotNull(ParameterExpression parameter)
            {
                return Expression.IfThen
                (
                    Expression.Equal(parameter, Expression.Constant(null, parameter.Type)),
                    Expression.Throw
                    (
                        Expression.Constant(new ArgumentNullException(parameter.Name))
                    )
                );
            }

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
                methods = new string[] { "GET" };

            foreach (string method in methods)
            {
                if (string.IsNullOrEmpty(method))
                    throw new ArgumentException(Resources.EMPTY_METHOD, nameof(methods));
#if NETSTANDARD2_1_OR_GREATER
                if (target.Handlers.TryAdd(method, handlerExpr))
                    continue;
#else
                if (!target.Handlers.ContainsKey(method))
                {
                    target.Handlers.Add(method, handlerExpr);
                    continue;
                }
#endif
                throw new ArgumentException(string.Format(Resources.Culture, Resources.ROUTE_ALREADY_REGISTERED, route), nameof(route));
            }

            foreach (string variable in route.Parameters.Keys)
            {
                FParameters.RegisterKey(variable);
            }
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
