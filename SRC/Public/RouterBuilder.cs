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
using System.Reflection;

namespace Solti.Utils.Router
{
    using Internals;
    using Primitives;
    using Properties;

    /// <summary>
    /// Builds the <see cref="Router"/> delegate that does the actual routing.
    /// <code>
    /// object Route(object? userData, string path, string method)
    /// { 
    ///     PathSplitter segments = PathSplitter.Split(path);
    ///     Dictionary&lt;string, object?&gt; paramz = new(MaxParameters);
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
    ///                         return DefaultHandler(userData, path);
    ///                     }
    /// 
    ///                     if (method == "GET")
    ///                     {
    ///                         return CicaMicaHandler(paramz, userData, path); // GET "/cica/mica" defined
    ///                     }
    ///                     
    ///                     return DefaultHandler(userData, path);  // Unknown method
    ///                 }
    ///                     
    ///                 if (intParser(segments.Current, out converted))
    ///                 {
    ///                     paramz.Add("id", val);
    ///                         
    ///                     if (segments.MoveNext())
    ///                     {
    ///                         return DefaultHandler(userData, path);
    ///                     }
    /// 
    ///                     if (method == "GET" || method == "POST" )
    ///                     {
    ///                         return CicaIdHandler(paramz, userData, path); // GET | POST "/cica/{id:int}" defined
    ///                     }
    ///                     
    ///                     return DefaultHandler(userData, path);
    ///                 }
    ///                     
    ///                 return DefaultHandler(userData, path);  // neither "/cica/mica" nor "/cica/{id:int}"
    ///             }
    ///             
    ///             if (method == "GET")
    ///             {
    ///                 return CicaHandler(paramz, userData, path); // GET "/cica" defined
    ///             }
    ///             
    ///             return DefaultHandler(userData, path);
    ///         }
    ///             
    ///         return DefaultHandler(userData, path);  // not "/cica[/..]"
    ///     }
    ///         
    ///     if (method == "GET")
    ///     {
    ///         return RootHandler(paramz, userData, path);  // GET "/" is defined
    ///     }
    ///     
    ///     return DefaultHandler(userData, path);
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
            public IDictionary<string, RequestHandler> Handlers { get; } = new Dictionary<string, RequestHandler>(StringComparer.OrdinalIgnoreCase);
            public IList<Junction> Children { get; } = new List<Junction>();
        }

        private sealed class BuildContext
        {        
            public ParameterExpression UserData { get; } = Expression.Parameter(typeof(object), nameof(UserData).ToLower());
            public ParameterExpression Path     { get; } = Expression.Parameter(typeof(string), nameof(Path).ToLower());
            public ParameterExpression Method   { get; } = Expression.Parameter(typeof(string), nameof(Method).ToLower());

            public ParameterExpression Segments  { get; } = Expression.Variable(typeof(PathSplitter), nameof(Segments).ToLower());
            public ParameterExpression Params    { get; } = Expression.Variable(typeof(Dictionary<string, object?>), nameof(Params).ToLower());
            public ParameterExpression Converted { get; } = Expression.Variable(typeof(object), nameof(Converted).ToLower());

            public LabelTarget Exit { get; } = Expression.Label(typeof(object), nameof(Exit));
        };

        private static readonly MethodInfo FMoveNext =
        (
            (MethodCallExpression)
            (
                (Expression<Action<PathSplitter>>)
                (
                    static enumerator => enumerator.MoveNext()
                )
            ).Body
        ).Method;

        private static readonly MethodInfo FSplit =
        (
            (MethodCallExpression) 
            (
                (Expression<Action>) 
                (
                    static () => PathSplitter.Split(null!)
                )
            ).Body
        ).Method;

        private static readonly MethodInfo FAddParam =
        (
            (MethodCallExpression)
            (
                (Expression<Action<Dictionary<string, object?>>>)
                (
                    static dict => dict.Add(null!, null)
                )
            ).Body
        ).Method;

        private static readonly MethodInfo FEquals =
        (
            (MethodCallExpression)
            (
                (Expression<Action<string>>)
                (
                    static s => s.Equals(null!, default(StringComparison))
                )
            ).Body
        ).Method;

        private static readonly PropertyInfo FCurrent = (PropertyInfo) 
        (
            (MemberExpression) 
            (
                (Expression<Func<PathSplitter, string>>)
                (
                    static enumerator => enumerator.Current
                )
            ).Body
        ).Member;

        private readonly RouteParser FRouteParser;

        private readonly Junction FRoot = new();

        private int FMaxParameters;

        private Expression BuildJunction(Junction junction, BuildContext context)
        {
            if (junction.Segment is null)  // root node, no segment
                return Expression.Block
                (
                    ProcessJunction().Append
                    (
                        Return
                        (
                            Expression.Invoke(Expression.Constant(DefaultHandler), context.UserData)
                        )
                    )
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
                Expression.Invoke
                (
                    Expression.Constant(junction.Segment.Converter),
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
                //     return DefaultHandler(...);
                // }
                //

                yield return Expression.IfThen
                (
                    Expression.Call(context.Segments, FMoveNext),
                    Expression.Block
                    (
                        junction
                            .Children
                            .Select(junction => BuildJunction(junction, context))
                            .Append
                            (
                                Return
                                (
                                    Expression.Invoke(Expression.Constant(DefaultHandler), context.UserData)
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

                foreach (IGrouping<RequestHandler, KeyValuePair<string, RequestHandler>> handlerGroup in junction.Handlers.GroupBy(static handler => handler.Value))
                {
                    yield return Expression.IfThen
                    (
                        handlerGroup
                            .Select(handler => Equals(context.Method, Expression.Constant(handler.Key)))
                            .Aggregate(static (accu, curr) => Expression.Or(accu, curr)),
                        Return
                        (
                            Expression.Invoke
                            (
                                Expression.Constant(handlerGroup.Key),
                                context.Params,
                                context.UserData
                            )
                        )
                    );
                }
            }

            static Expression Equals(Expression left, Expression right) => Expression.Call
            (
                left,
                FEquals,
                right,
                Expression.Constant(StringComparison.OrdinalIgnoreCase)
            );

            Expression Return(InvocationExpression invocation) => Expression.Return
            (
                context.Exit,
                invocation,
                typeof(object)
            );
        }
        #endregion

        /// <summary>
        /// Creates a new <see cref="RouterBuilder"/> instance.
        /// </summary>
        /// <param name="defaultHandler">Delegate to handle unknown routes.</param>
        /// <param name="converters">Converters to be used during parameter resolution. If null, <see cref="DefaultConverters"/> will be sued.</param>
        public RouterBuilder(DefaultRequestHandler defaultHandler, IReadOnlyDictionary<string, ConverterFactory>? converters = null)
        {
            DefaultHandler = defaultHandler ?? throw new ArgumentNullException(nameof(defaultHandler));
            FRouteParser = new RouteParser(converters ?? converters ?? DefaultConverters.Instance);
        }

        /// <summary>
        /// Builds the actual <see cref="Router"/> delegate.
        /// </summary>
        public Router Build()
        {
            BuildContext context = new();

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
                        Expression.Call(FSplit, context.Path)
                    ),
                    Expression.Assign
                    (
                        context.Params,
                        Expression.New
                        (
                            typeof(Dictionary<string, object?>).GetConstructor(new Type[] { typeof(int) }), // ctor(capacity)
                            Expression.Constant(FMaxParameters)
                        )
                    ),
                    BuildJunction(FRoot, context),
                    Expression.Label(context.Exit, Expression.Constant(null, typeof(object)))
                ),
                parameters: new ParameterExpression[]
                {
                    context.UserData,
                    context.Path,
                    context.Method
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
        public DefaultRequestHandler DefaultHandler { get; }

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered.</param>
        /// <param name="handler">Function accepting requests on the given route.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(string route, RequestHandler handler, params string[] methods)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            if (methods is null)
                throw new ArgumentNullException(nameof(methods));

            Junction target = FRoot;

            int parameters = 0;

            foreach (RouteSegment segment in FRouteParser.Parse(route))
            {
                bool found = false;

                foreach (Junction child in target.Children)
                {
                    Debug.Assert(child.Segment is not null, "Root cannot be a child");

                    if
                    (
                        (segment.Converter is null && segment.Name.Equals(child.Segment!.Name, StringComparison.OrdinalIgnoreCase)) || 
                        (segment.Converter is not null && segment.Converter.Method == child.Segment!.Converter?.Method)
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

                if (segment.Converter is not null)
                    parameters++;
            }

            if (methods.Length is 0)
                methods = new string[] { "GET" };

            foreach (string method in methods)
            {
                if (!target.Handlers.TryAdd(method, handler))
                    throw new ArgumentException(string.Format(Resources.Culture, Resources.ROUTE_ALREADY_REGISTERED, route), nameof(route));
            }

            FMaxParameters = Math.Max(FMaxParameters, parameters);
        }
    }
}
