/********************************************************************************
* IRouterBuilder.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.Router
{
    /// <summary>
    /// Defines the contract of router builders
    /// </summary>
    /// <remarks>This interface is intended for testing purposes only</remarks>
    public interface IRouterBuilder
    {
        /// <summary>
        /// Registers a new route
        /// </summary>
        /// <param name="route">Route to be registered. Must NOT include the base URL.</param>
        /// <param name="handlerExpr">Function accepting requests on the given route. You may pass async and sync callbacks as well.</param>
        /// <param name="methods">Accepted HTTP methods for this route. If omitted "GET" will be used.</param>
        void AddRoute(ParsedRoute route, LambdaExpression handlerExpr, params string[] methods);
    }
}
