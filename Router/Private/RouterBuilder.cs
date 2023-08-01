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

namespace Router.Internals
{
    using Properties;

    /// <summary>
    /// Builds the switch statement which does the actual routing.
    /// <code>
    /// TResponse Route(string path, TRequest request, TUserData userData)
    /// {
    ///     Dictionary&lt;string, object?&gt; paramz = new();
    ///     object converted;
    ///     
    ///     using(IEnumerator&lt;string&gt; segments = PathSplitter.Split(path).GetEnumerator())
    ///     {
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
    ///                         if (segments.MoveNext())
    ///                         {
    ///                             return DefaultHandler(request, userData, path);
    ///                         }
    /// 
    ///                         paramz.Add("id", val);
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
    internal class RouterBuilder<TRequest, TUserData, TResponse>
    {
        private sealed class Junction
        {
            public RouteSegment? Segment { get; init; }  // null at "/"

            public Handler<TRequest, TUserData, TResponse>? Handler { get; set; }

            public IList<Junction> Children { get; } = new List<Junction>();
        }

        private sealed record CaseContext
        (
            ParameterExpression Segments,
            ParameterExpression Request,
            ParameterExpression Params,
            ParameterExpression UserData,
            ParameterExpression Path,
            ParameterExpression Converted
        );

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

        private static readonly MethodInfo FDispose =
        (
            (MethodCallExpression)
            (
                (Expression<Action<IEnumerator<string>>>)
                (
                    static enumerator => enumerator.Dispose()
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

        private readonly RouteParser FRouteParser = null!;


        private static IEnumerator<string> GetSegments(string path) => PathSplitter.Split(path).GetEnumerator();

        private readonly DefaultHandler<TRequest, TUserData, TResponse> FDefaultHandler = null!;

        private Expression DefineCase
        (
            CaseContext context,
            Junction junction
        )
        {
            Expression tryProcessNextJunction = Expression.IfThen
            (
                Expression.Call(context.Segments, FMoveNext),
                Expression.Block
                (
                    type: typeof(TResponse),
                    junction
                        .Children
                        .Select(junction => DefineCase(context, junction))
                        // default handler
                        .Append(Expression.Invoke(Expression.Constant(FDefaultHandler), context.Request, context.UserData, context.Path))
                )
            );

            if (junction.Segment is null)  // root node, no segment
            {
                return Expression.Block
                (
                    type: typeof(TResponse),
                    tryProcessNextJunction,
                    junction.Handler is not null
                        ? Expression.Invoke(Expression.Constant(junction.Handler), context.Request, context.Params, context.UserData, context.Path)
                        : Expression.Invoke(Expression.Constant(FDefaultHandler), context.Request, context.UserData, context.Path)
                );
            }

            Debug.Assert(junction.Handler is not null, "Handler cannot be null");

            return junction.Segment.Converter is null
                ? Expression.IfThen
                (
                    Expression.Equal
                    (
                        Expression.Property(context.Segments, FCurrent),
                        Expression.Constant(junction.Segment.Name)
                    ),
                    Expression.Block
                    (
                        type: typeof(TResponse),
                        tryProcessNextJunction,
                        Expression.Invoke(Expression.Constant(junction.Handler), context.Request, context.Params, context.UserData, context.Path)
                    )
                )
                : Expression.IfThen
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
                        tryProcessNextJunction,
                        Expression.Call(context.Params, FAddParam, Expression.Constant(junction.Segment.Name), context.Converted),
                        Expression.Invoke(Expression.Constant(junction.Handler), context.Request, context.Params, context.UserData, context.Path)
                    )
                );
        }

        private readonly Junction FRoot = new();

        public void AddRoute(string route, Handler<TRequest, TUserData, TResponse> handler)
        {
            Junction target = FRoot;

            foreach (RouteSegment segment in FRouteParser.Parse(route))
            {
                bool found = false;

                foreach (Junction child in target.Children)
                {
                    Debug.Assert(child.Segment is not null, "Root cannot be a child");

                    if (child.Segment!.Name == segment.Name)
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
