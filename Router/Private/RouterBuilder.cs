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
    /// TResponse Route(TRequest request, TUserData userData, string path)
    /// { 
    ///     using(IEnumerator&lt;string&gt; segments = PathSplitter.Split(path).GetEnumerator())
    ///     {
    ///         Dictionary&lt;string, object?&gt; paramz = new();
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
    ///                             return DefaultHandler(request, userData, path);
    ///                         }
    /// 
    ///                         return CicaMicaHandler(request, paramz, userData, path); // "/cica/mica" defined
    ///                     }
    ///                     
    ///                     if (intParser(segments.Current, out converted))
    ///                     {
    ///                         paramz.Add("id", val);
    ///                         
    ///                         if (segments.MoveNext())
    ///                         {
    ///                             return DefaultHandler(request, userData, path);
    ///                         }
    /// 
    ///                         return CicaIdHandler(request, paramz, userData, path); // "/cica/{id:int}" defined
    ///                     }
    ///                     
    ///                     return DefaultHandler(request, userData, path);  // neither "/cica/mica" nor "/cica/{id:int}"
    ///                 }
    ///                 
    ///                 return CicaHandler(request, paramz, userData, path); // "/cica" defined
    ///             }
    ///             
    ///             return DefaultHandler(request, userData, path);  // not "/cica[/..]"
    ///         }
    ///         
    ///         return RootHandler(request, paramz, userData, path);  // "/" is defined
    ///     }
    /// }
    /// </code>
    /// </summary>
    internal sealed class RouterBuilder<TRequest, TUserData, TResponse>
    {
        #region Private
        private sealed class Junction
        {
            public RouteSegment? Segment { get; init; }  // null at "/"

            public Handler<TRequest, TUserData, TResponse>? Handler { get; set; }

            public IList<Junction> Children { get; } = new List<Junction>();
        }

        private sealed record BuildContext
        (
            ParameterExpression Segments,
            ParameterExpression Request,
            ParameterExpression Params,
            ParameterExpression UserData,
            ParameterExpression Path,
            ParameterExpression Converted
        )
        {
            public BuildContext(): this
            (
                Converted: Expression.Variable(typeof(object),                      nameof(Converted).ToLower()),
                Params:    Expression.Variable(typeof(Dictionary<string, object?>), nameof(Params).ToLower()),

                Segments: Expression.Parameter(typeof(IEnumerator<string>), nameof(Segments).ToLower()),
                Request:  Expression.Parameter(typeof(TRequest),            nameof(Request).ToLower()),
                UserData: Expression.Parameter(typeof(TUserData),           nameof(UserData).ToLower()),
                Path:     Expression.Parameter(typeof(string),              nameof(Path).ToLower())
            ) {}
        };

        private static readonly MethodInfo FMoveNext =
        (
            (MethodCallExpression) 
            (
                (Expression<Action<IEnumerator<string>>>) 
                (
                    static enumerator => enumerator.MoveNext()
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
                (Expression<Func<IEnumerator<string>, string>>)
                (
                    static enumerator => enumerator.Current
                )
            ).Body
        ).Member;

        private readonly RouteParser FRouteParser;

        private readonly DefaultHandler<TRequest, TUserData, TResponse> FDefaultHandler;

        private readonly Junction FRoot = new();

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
                            .Append(Expression.Invoke(Expression.Constant(FDefaultHandler), context.Request, context.UserData, context.Path))
                    )
                ),
                invokeHandler = junction.Handler is not null
                    ? Expression.Invoke(Expression.Constant(junction.Handler), context.Request, context.Params, context.UserData, context.Path)
                    : Expression.Invoke(Expression.Constant(FDefaultHandler), context.Request, context.UserData, context.Path);

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
                    Expression.Constant(junction.Segment.ConverterParam),
                    context.Converted
                ),
                Expression.Block
                (
                    type: typeof(TResponse),
                    tryProcessNextJunction,
                    Expression.Call(context.Params, FAddParam, Expression.Constant(junction.Segment.Name), context.Converted),
                    invokeHandler
                )
            );
        }
        #endregion

        public RouterBuilder(DefaultHandler<TRequest, TUserData, TResponse> defaultHandler, IReadOnlyDictionary<string, TryConvert> converters)
        {
            FDefaultHandler = defaultHandler;
            FRouteParser = new RouteParser(converters);
        }

        /// <summary>
        /// Builds the actual <see cref="Router{TRequest, TUserData, TResponse}"/>.
        /// </summary>
        public Router<TRequest, TUserData, TResponse> Build()
        {
            BuildContext context = new();

            Expression<Func<TRequest, IEnumerator<string>, TUserData, string, TResponse>> coreExpr = Expression.Lambda<Func<TRequest, IEnumerator<string>, TUserData, string, TResponse>>
            (
                body: Expression.Block
                (
                    type: typeof(TResponse),
                    variables: new ParameterExpression[]
                    {
                        context.Params,
                        context.Converted
                    },
                    Expression.Assign(context.Params, Expression.New(typeof(Dictionary<string, object?>))),
                    BuildJunction(FRoot, context)
                ),
                parameters: new ParameterExpression[]
                {
                    context.Request,
                    context.Segments,
                    context.UserData,
                    context.Path
                }
            );

            Debug.WriteLine(coreExpr.GetDebugView());

            Func<TRequest, IEnumerator<string>, TUserData, string, TResponse> core = coreExpr.Compile();

            return (TRequest request, TUserData userData, string path) =>
            {
                using IEnumerator<string> segments = PathSplitter.Split(path).GetEnumerator();
                return core(request, segments, userData, path);
            };
        }

        public StringComparison StringComparison { get; init; } = StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Registers a route.
        /// </summary>
        /// <param name="route">Route to be registered.</param>
        /// <param name="handler">Function accepting requests on the given route.</param>
        /// <exception cref="ArgumentException">If the route already registered.</exception>
        public void AddRoute(string route, Handler<TRequest, TUserData, TResponse> handler)
        {
            Junction target = FRoot;

            foreach (RouteSegment segment in FRouteParser.Parse(route))
            {
                bool found = false;

                foreach (Junction child in target.Children)
                {
                    Debug.Assert(child.Segment is not null, "Root cannot be a child");

                    if (child.Segment!.Name.Equals(segment.Name, StringComparison) || (segment.Converter is not null && segment.Converter == child.Segment.Converter))
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

            if (target.Handler is not null)
                throw new ArgumentException(string.Format(Resources.Culture, Resources.ROUTE_ALREADY_REGISTERED, route), nameof(route));

            target.Handler = handler;
        }
    }
}
