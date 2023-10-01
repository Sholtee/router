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
    ///     PathSplitter segments = PathSplitter.Split(path, splitOptions);
    ///     StaticDictionary paramz = createParamzDict();
    ///     object converted;
    ///    
    ///     if (segments.MoveNext())
    ///     {
    ///         if (segments.Current == "cica")
    ///         {
    ///             if (segments.MoveNext())
    ///             {
    ///                 if (segments.Current == "mica")
    ///                 {
    ///                     if (segments.MoveNext())
    ///                     {
    ///                         return DefaultHandler(userData, HttpStatusCode.NotFound);
    ///                     }
    /// 
    ///                     if (method == "GET")
    ///                     {
    ///                         return CicaMicaHandler(paramz, userData); // GET "/cica/mica" defined
    ///                     }
    ///                     
    ///                     return DefaultHandler(userData, HttpStatusCode.MethodNotAllowed);  // Unknown method
    ///                 }
    ///                     
    ///                 if (intParser(segments.Current, out converted))
    ///                 {
    ///                     paramz.Add("id", val);
    ///                         
    ///                     if (segments.MoveNext())
    ///                     {
    ///                         return DefaultHandler(userData, HttpStatusCode.NotFound);
    ///                     }
    /// 
    ///                     if (method == "GET" || method == "POST" )
    ///                     {
    ///                         return CicaIdHandler(paramz, userData); // GET | POST "/cica/{id:int}" defined
    ///                     }
    ///                     
    ///                     return DefaultHandler(userData, HttpStatusCode.MethodNotAllowed);
    ///                 }
    ///                     
    ///                 return DefaultHandler(userData, HttpStatusCode.NotFound);  // neither "/cica/mica" nor "/cica/{id:int}"
    ///             }
    ///             
    ///             if (method == "GET")
    ///             {
    ///                 return CicaHandler(paramz, userData); // GET "/cica" defined
    ///             }
    ///             
    ///             return DefaultHandler(userData, HttpStatusCode.MethodNotAllowed);
    ///         }
    ///             
    ///         return DefaultHandler(userData, HttpStatusCode.NotFound);  // not "/cica[/..]"
    ///     }
    ///         
    ///     if (method == "GET")
    ///     {
    ///         return RootHandler(paramz, userData);  // GET "/" is defined
    ///     }
    ///     
    ///     return DefaultHandler(userData, HttpStatusCode.MethodNotAllowed);
    /// }
    /// </code>
    /// </summary>
    public sealed class RouterBuilder
    {
        #region Private
        private sealed class Junction
        {
            public RouteSegment? Segment { get; init; }  // null at "/"
            // order doesn't matter
            public IDictionary<string, Expression<RequestHandler>> Handlers { get; } = new Dictionary<string, Expression<RequestHandler>>
            (
                StringComparer.OrdinalIgnoreCase
            );
            public IList<Junction> Children { get; } = new List<Junction>();
        }

        private sealed class BuildContext
        {
            public ParameterExpression UserData     { get; } = Expression.Parameter(typeof(object), nameof(UserData).ToLower());
            public ParameterExpression Path         { get; } = Expression.Parameter(typeof(string), nameof(Path).ToLower());
            public ParameterExpression Method       { get; } = Expression.Parameter(typeof(string), nameof(Method).ToLower());
            public ParameterExpression SplitOptions { get; } = Expression.Parameter(typeof(SplitOptions), nameof(SplitOptions).ToLower());

            public ParameterExpression Segments  { get; } = Expression.Variable(typeof(PathSplitter), nameof(Segments).ToLower());
            public ParameterExpression Params    { get; } = Expression.Variable(typeof(StaticDictionary), nameof(Params).ToLower());
            public ParameterExpression Converted { get; } = Expression.Variable(typeof(object), nameof(Converted).ToLower());

            public LabelTarget Exit { get; } = Expression.Label(typeof(object), nameof(Exit));
        };

        private static readonly MethodInfo
            FMoveNext = MethodInfoExtractor.Extract<PathSplitter>(static parts => parts.MoveNext()),
            FSplit    = MethodInfoExtractor.Extract(static () => PathSplitter.Split(null!, SplitOptions.Default)),
            FAddParam = MethodInfoExtractor.Extract<StaticDictionary>(static dict => dict.Add(null!, null)),
            FEquals   = MethodInfoExtractor.Extract<string>(static s => s.Equals(null!, default(StringComparison))),
            FConvert  = MethodInfoExtractor.Extract<IConverter, object?>(static (c, output) => c.ConvertToValue(null!, out output));

        private static readonly PropertyInfo
            FCurrent = PropertyInfoExtractor.Extract<PathSplitter, string>(static parts => parts.Current);

        private readonly Junction FRoot = new();

        private readonly IReadOnlyDictionary<string, ConverterFactory> FConverters;

        private readonly StaticDictionaryBuilder FStaticDictionaryBuilder = new();

        private Expression BuildJunction(Junction junction, BuildContext context)
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
                        Expression.Property(context.Segments, FCurrent),
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
                    Expression.Property(context.Segments, FCurrent),
                    context.Converted
                ),
                Expression.Block
                (
                    new Expression[]
                    {
                        Expression.Call
                        (
                            context.Params,
                            FAddParam,
                            Expression.Constant(junction.Segment.Name),
                            context.Converted
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
                    Expression.Call(context.Segments, FMoveNext),
                    Expression.Block
                    (
                        junction
                            .Children
                            .Select(child => BuildJunction(child, context))
                            .Append
                            (
                                Return
                                (
                                    DefaultHandler,
                                    context.UserData,
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
                            .Select(handler => Equals(context.Method, Expression.Constant(handler.Key)))
                            .Aggregate(static (accu, curr) => Expression.Or(accu, curr)),
                        Return
                        (
                            handlerGroup.Key,
                            context.Params,
                            context.UserData
                        )
                    );
                }

                //
                // return DEfaultHandler(state, HttpStatusCode.[xXx]);
                //

                yield return Return
                (
                    DefaultHandler,
                    context.UserData,
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

            Expression Return(LambdaExpression lambda, params Expression[] paramz) => Expression.Return
            (
                context.Exit,
                UnfoldedLambda.Create
                (
                    lambda,
                    paramz
                ),
                typeof(object)
            );
        }
        #endregion

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="handlerExpr">Delegate to handle unknown routes.</param>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be used.</param>
        public RouterBuilder(Expression<DefaultRequestHandler> handlerExpr, IReadOnlyDictionary<string, ConverterFactory>? converters = null)
        {
            DefaultHandler = handlerExpr ?? throw new ArgumentNullException(nameof(handlerExpr));
            FConverters = converters ?? DefaultConverters.Instance;
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
        ) {}

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be used.</param>
        public RouterBuilder(IReadOnlyDictionary<string, ConverterFactory>? converters = null): this
        (
            //
            // Compiler generated expression tree cannot contain throw expression (CS8188)
            //

            handler: static (_, _) => throw new InvalidOperationException(Resources.ROUTE_NOT_REGISTERED),
            converters
        ) {}

        /// <summary>
        /// Builds the actual <see cref="Router"/> delegate.
        /// </summary>
        public Router Build()
        {
            BuildContext context = new();

            StaticDictionaryFactory createParamzDict = FStaticDictionaryBuilder.CreateFactory(); 

            Expression<Router> routerExpr = Expression.Lambda<Router>
            (
                body: Expression.Block
                (
                    type: typeof(object),
                    variables: new ParameterExpression[]
                    {
                        context.Segments,
                        context.Params,
                        context.Converted
                    },
                    EnsureNotNull(context.Path),
                    EnsureNotNull(context.Method),
                    Expression.Assign
                    (
                        context.Segments,
                        Expression.Call(FSplit, context.Path, context.SplitOptions)
                    ),
                    Expression.Assign
                    (
                        context.Params,
                        Expression.Invoke
                        (
                            Expression.Constant(createParamzDict)
                        )
                    ),
                    BuildJunction(FRoot, context),
                    Expression.Label(context.Exit, Expression.Constant(null, typeof(object)))
                ),
                parameters: new ParameterExpression[]
                {
                    context.UserData,
                    context.Path,
                    context.Method,
                    context.SplitOptions
                }
            );

            Debug.WriteLine(routerExpr.GetDebugView());

            return routerExpr.Compile();

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
        }

        /// <summary>
        /// Delegate that handles the unknown routes.
        /// </summary>
        public Expression<DefaultRequestHandler> DefaultHandler { get; }

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handlerExpr">Function accepting requests on the given route.</param>
        /// <param name="splitOptions">Specifies how to split the <paramref name="route"/>.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(string route, Expression<RequestHandler> handlerExpr, SplitOptions splitOptions, params string[] methods)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            if (handlerExpr is null)
                throw new ArgumentNullException(nameof(handlerExpr));

            if (methods is null)
                throw new ArgumentNullException(nameof(methods));


            ParsedRoute parsedRoute = RouteTemplate.Parse(route, FConverters, splitOptions);
            Junction target = FRoot;

            foreach (RouteSegment segment in parsedRoute.Segments)
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
#if !NETSTANDARD2_1_OR_GREATER
                if (!target.Handlers.ContainsKey(method))
                {
                    target.Handlers.Add(method, handlerExpr);
                    continue;
                }
#else
                if (!target.Handlers.TryAdd(method, handlerExpr))
#endif
                    throw new ArgumentException(string.Format(Resources.Culture, Resources.ROUTE_ALREADY_REGISTERED, route), nameof(route));
            }

            foreach (string variable in parsedRoute.Variables.Keys)
            {
                FStaticDictionaryBuilder.RegisterKey(variable);
            }
        }

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
    }
}
