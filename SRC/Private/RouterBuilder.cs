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

namespace Solti.Utils.Router.Internals
{
    using Primitives;
    using Properties;

    /// <summary>
    /// Builds the switch statement which does the actual routing.
    /// <code>
    /// TResponse Route(TRequest request, TUserData? userData, string path)
    /// { 
    ///     PathSplitter segments = PathSplitter.Split(path);
    ///     Dictionary&lt;string, object?&gt; paramz = new();
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
    ///                         return DefaultHandler(request, userData, path);
    ///                     }
    /// 
    ///                     return CicaMicaHandler(request, paramz, userData, path); // "/cica/mica" defined
    ///                 }
    ///                     
    ///                 if (intParser(segments.Current, out converted))
    ///                 {
    ///                     paramz.Add("id", val);
    ///                         
    ///                     if (segments.MoveNext())
    ///                     {
    ///                         return DefaultHandler(request, userData, path);
    ///                     }
    /// 
    ///                     return CicaIdHandler(request, paramz, userData, path); // "/cica/{id:int}" defined
    ///                 }
    ///                     
    ///                 return DefaultHandler(request, userData, path);  // neither "/cica/mica" nor "/cica/{id:int}"
    ///             }
    ///                 
    ///             return CicaHandler(request, paramz, userData, path); // "/cica" defined
    ///         }
    ///             
    ///         return DefaultHandler(request, userData, path);  // not "/cica[/..]"
    ///     }
    ///         
    ///     return RootHandler(request, paramz, userData, path);  // "/" is defined
    /// }
    /// </code>
    /// </summary>
    internal sealed class RouterBuilder<TRequest, TUserData, TResponse>
    {
        #region Private
        private sealed class Junction
        {
            public RouteSegment? Segment { get; init; }  // null at "/"
            public Handler<TRequest, TUserData?, TResponse>? Handler { get; set; }
            public IList<Junction> Children { get; } = new List<Junction>();
        }

        private sealed class BuildContext
        {        
            public ParameterExpression Request  { get; } = Expression.Parameter(typeof(TRequest), nameof(Request).ToLower());
            public ParameterExpression UserData { get; } = Expression.Parameter(typeof(TUserData?), nameof(UserData).ToLower());
            public ParameterExpression Path     { get; } = Expression.Parameter(typeof(string), nameof(Path).ToLower());

            public ParameterExpression Segments  { get; } = Expression.Variable(typeof(PathSplitter), nameof(Segments).ToLower());
            public ParameterExpression Params    { get; } = Expression.Variable(typeof(Dictionary<string, object?>), nameof(Params).ToLower());
            public ParameterExpression Converted { get; } = Expression.Variable(typeof(object), nameof(Converted).ToLower());

            public LabelTarget Exit { get; } = Expression.Label(typeof(TResponse), nameof(Exit));
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
                        type: typeof(TResponse),
                        junction
                            .Children
                            .Select(junction => BuildJunction(junction, context))
                            // default handler
                            .Append
                            (
                                Return
                                (
                                    Expression.Invoke(Expression.Constant(DefaultHandler), context.Request, context.UserData, context.Path)
                                )
                            )
                    )
                ),
                invokeHandler = Return
                (
                    junction.Handler is not null
                        ? Expression.Invoke(Expression.Constant(junction.Handler), context.Request, context.Params, context.UserData, context.Path)
                        : Expression.Invoke(Expression.Constant(DefaultHandler), context.Request, context.UserData, context.Path)
                );

            if (junction.Segment is null)  // root node, no segment
                return Expression.Block
                (
                    type: typeof(TResponse),
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
                        type: typeof(TResponse),
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
                    type: typeof(TResponse),
                    Expression.Call(context.Params, FAddParam, Expression.Constant(junction.Segment.Name), context.Converted),
                    tryProcessNextJunction,
                    invokeHandler
                )
            );

            Expression Return(InvocationExpression invocation) => Expression.Return
            (
                context.Exit,
                invocation,
                typeof(TResponse)
            );
        }
        #endregion

        public RouterBuilder(DefaultHandler<TRequest, TUserData?, TResponse> defaultHandler, IReadOnlyDictionary<string, ConverterFactory> converters, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            DefaultHandler = defaultHandler;
            FRouteParser = new RouteParser(converters, StringComparison = stringComparison);
        }

        /// <summary>
        /// Builds the actual <see cref="Router{TRequest, TUserData?, TResponse}"/>.
        /// </summary>
        public Router<TRequest, TUserData?, TResponse> Build()
        {
            BuildContext context = new();

            Expression<Router<TRequest, TUserData?, TResponse>> routerExpr = Expression.Lambda<Router<TRequest, TUserData?, TResponse>>
            (
                body: Expression.Block
                (
                    type: typeof(TResponse),
                    variables: new ParameterExpression[]
                    {
                        context.Segments,
                        context.Params,
                        context.Converted
                    },
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
                    Expression.Label(context.Exit, Expression.Constant(default(TResponse)))
                ),
                parameters: new ParameterExpression[]
                {
                    context.Request,
                    context.UserData,
                    context.Path
                }
            );

            Debug.WriteLine(routerExpr.GetDebugView());

            return routerExpr.Compile();
        }

        public DefaultHandler<TRequest, TUserData?, TResponse> DefaultHandler { get; }

        public StringComparison StringComparison { get; }

        /// <summary>
        /// Registers a route.
        /// </summary>
        /// <param name="route">Route to be registered.</param>
        /// <param name="handler">Function accepting requests on the given route.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(string route, Handler<TRequest, TUserData?, TResponse> handler)
        {
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
