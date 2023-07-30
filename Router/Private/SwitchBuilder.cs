/********************************************************************************
* SwitchBuilder.cs                                                              *
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
    internal class SwitchBuilder<TRequest, TUserData, TResponse>
    {
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

        private sealed class Junction
        {
            public IEnumerable<Junction> Walk()
            {
                yield break;
            }

            public RouteSegment? Segment { get; }  // null at "/"

            public Handler<TRequest, TUserData, TResponse>? Handler { get; }  // may be null at "/"
        }

        private Expression DefineCase
        (
            ParameterExpression segments,
            ParameterExpression request,
            ParameterExpression paramz,
            ParameterExpression userData,
            ParameterExpression path,
            ParameterExpression converted,
            Junction junction
        )
        {
            Expression tryProcessNextJunction = Expression.IfThen
            (
                Expression.Call(segments, FMoveNext),
                Expression.Block
                (
                    type: typeof(TResponse),
                    junction
                        .Walk()
                        // next junctions...
                        .Select(junction => DefineCase(segments, request, paramz, userData, path, converted, junction))
                        // default handler
                        .Append(Expression.Invoke(Expression.Constant(FDefaultHandler), request, userData, path))
                )
            );


            if (junction.Segment is null)  // root node, no segment
            {
                return Expression.Block
                (
                    type: typeof(TResponse),
                    tryProcessNextJunction,
                    junction.Handler is not null
                        ? Expression.Invoke(Expression.Constant(junction.Handler), request, paramz, userData, path)
                        : Expression.Invoke(Expression.Constant(FDefaultHandler), request, userData, path)
                );
            }

            Debug.Assert(junction.Handler is not null, "Handler cannot be null");

            return junction.Segment.Converter is null
                ? Expression.IfThen
                (
                    Expression.Equal
                    (
                        Expression.Property(segments, FCurrent),
                        Expression.Constant(junction.Segment.Name)
                    ),
                    Expression.Block
                    (
                        type: typeof(TResponse),
                        tryProcessNextJunction,
                        Expression.Invoke(Expression.Constant(junction.Handler), request, paramz, userData, path)
                    )
                )
                : Expression.IfThen
                (
                    Expression.Invoke
                    (
                        Expression.Constant(junction.Segment.Converter),
                        Expression.Property(segments, FCurrent),
                        converted
                    ),
                    Expression.Block
                    (
                        type: typeof(TResponse),
                        tryProcessNextJunction,
                        Expression.Call(paramz, FAddParam, Expression.Constant(junction.Segment.Name), converted),
                        Expression.Invoke(Expression.Constant(junction.Handler), request, paramz, userData, path)
                    )
                );
        }

        public void AddRoute(string route)
        {
            IEnumerable<RouteSegment> segments = FRouteParser.Parse(route);

        }
    }
}
