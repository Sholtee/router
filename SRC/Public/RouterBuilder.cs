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
    /// object Route(object? userData, string path)
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
    ///                     return CicaMicaHandler(paramz, userData, path); // "/cica/mica" defined
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
    ///                     return CicaIdHandler(paramz, userData, path); // "/cica/{id:int}" defined
    ///                 }
    ///                     
    ///                 return DefaultHandler(userData, path);  // neither "/cica/mica" nor "/cica/{id:int}"
    ///             }
    ///                 
    ///             return CicaHandler(paramz, userData, path); // "/cica" defined
    ///         }
    ///             
    ///         return DefaultHandler(userData, path);  // not "/cica[/..]"
    ///     }
    ///         
    ///     return RootHandler(paramz, userData, path);  // "/" is defined
    /// }
    /// </code>
    /// </summary>
    public sealed class RouterBuilder
    {
        #region Private
        private sealed class Junction
        {
            public RouteSegment? Segment { get; init; }  // null at "/"
            public RequestHandler? Handler { get; set; }
            public IList<Junction> Children { get; } = new List<Junction>();
        }

        private sealed class BuildContext
        {        
            public ParameterExpression UserData { get; } = Expression.Parameter(typeof(object), nameof(UserData).ToLower());
            public ParameterExpression Path     { get; } = Expression.Parameter(typeof(string), nameof(Path).ToLower());

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
            Expression
                tryProcessNextJunction = Expression.IfThen
                (
                    Expression.Call(context.Segments, FMoveNext),
                    Expression.Block
                    (
                        type: typeof(object),
                        junction
                            .Children
                            .Select(junction => BuildJunction(junction, context))
                            // default handler
                            .Append
                            (
                                Return
                                (
                                    Expression.Invoke(Expression.Constant(DefaultHandler), context.UserData, context.Path)
                                )
                            )
                    )
                ),
                invokeHandler = Return
                (
                    junction.Handler is not null
                        ? Expression.Invoke(Expression.Constant(junction.Handler), context.Params, context.UserData, context.Path)
                        : Expression.Invoke(Expression.Constant(DefaultHandler), context.UserData, context.Path)
                );

            if (junction.Segment is null)  // root node, no segment
                return Expression.Block
                (
                    type: typeof(object),
                    tryProcessNextJunction,
                    invokeHandler
                );

            if (junction.Segment.Converter is null)
                return Expression.IfThen
                (
                    Expression.Call
                    (
                        Expression.Property(context.Segments, FCurrent),
                        FEquals,
                        Expression.Constant(junction.Segment.Name),
                        Expression.Constant(StringComparison)
                    ),
                    Expression.Block
                    (
                        type: typeof(object),
                        tryProcessNextJunction,
                        invokeHandler
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
                    type: typeof(object),
                    Expression.Call(context.Params, FAddParam, Expression.Constant(junction.Segment.Name), context.Converted),
                    tryProcessNextJunction,
                    invokeHandler
                )
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
        /// <param name="stringComparison">Case rules to be used (strongly advised to use the value of <see cref="StringComparison.OrdinalIgnoreCase"/>)</param>
        public RouterBuilder(DefaultRequestHandler defaultHandler, IReadOnlyDictionary<string, ConverterFactory>? converters = null, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            DefaultHandler = defaultHandler ?? throw new ArgumentNullException(nameof(defaultHandler));
            FRouteParser = new RouteParser
            (
                converters ?? converters ?? DefaultConverters.Instance,
                StringComparison = stringComparison
            );
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
                    Expression.IfThen
                    (
                        Expression.Equal(context.Path, Expression.Constant(null, context.Path.Type)),
                        Expression.Throw
                        (
                            Expression.Constant(new ArgumentNullException(context.Path.Name))
                        )
                    ),
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
                    context.Path
                }
            );

            Debug.WriteLine(routerExpr.GetDebugView());

            return routerExpr.Compile();
        }

        /// <summary>
        /// Delegate that handles the unknown routes.
        /// </summary>
        public DefaultRequestHandler DefaultHandler { get; }

        /// <summary>
        /// Case rules
        /// </summary>
        public StringComparison StringComparison { get; }

        /// <summary>
        /// Registers a new route.
        /// </summary>
        /// <param name="route">Route to be registered.</param>
        /// <param name="handler">Function accepting requests on the given route.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(string route, RequestHandler handler)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

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
                        (segment.Converter is null && segment.Name.Equals(child.Segment.Name, StringComparison)) || 
                        (segment.Converter is not null && segment.Converter.Method == child.Segment.Converter?.Method)
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

            if (target.Handler is not null)
                throw new ArgumentException(string.Format(Resources.Culture, Resources.ROUTE_ALREADY_REGISTERED, route), nameof(route));

            target.Handler = handler;

            FMaxParameters = Math.Max(FMaxParameters, parameters);
        }
    }
}
