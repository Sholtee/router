/********************************************************************************
* SwitchBuilder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Router.Internals
{
    /// <summary>
    /// Builds the switch statement which does the actual routing.
    /// <code>
    /// void Route(string path)
    /// {
    ///     Dictionary&lt;string, object?&gt; paramz = new();
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
    ///                         CicaMicaHandler(paramz, path); // "/cica/mica" defined
    ///                         return;
    ///                     }
    ///                     else if (intParser(segments.Current, out int val))
    ///                     {
    ///                         paramz.Add("id", val);
    ///                         CicaIdHandler(paramz, path); // "/cica/{id:int}" defined
    ///                         return;
    ///                     }
    ///                 }
    ///                 
    ///                 CicaHandler(paramz, path); // "/cica" defined
    ///                 return;
    ///             }
    ///         }
    ///         
    ///         DefaultHandler(path);  // "/" is not defined
    ///         return;
    ///     }
    /// }
    /// </code>
    /// </summary>
    internal class SwitchBuilder
    {
        private readonly RouteParser FRouteParser;

        public void AddRoute(string route)
        {
            IEnumerable<RouteSegment> segments = FRouteParser.Parse(route);

        }
    }
}
