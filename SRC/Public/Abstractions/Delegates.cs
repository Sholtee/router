/********************************************************************************
* Delegates.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Solti.Utils.Router
{
    /// <summary>
    /// Creates <see cref="IConverter"/> instances.
    /// </summary>
    /// <param name="style">Style to be used during formaiting.</param>
    public delegate IConverter ConverterFactory(string? style);

    /// <summary>
    /// Represents a template compiler function that is responsible for interpolating parameters in the encapsulated route template.
    /// </summary>
    /// <param name="paramz">Dictionary contaning the parameter values.</param>
    public delegate string RouteTemplateCompiler(IReadOnlyDictionary<string, object?> paramz);

    /// <summary>
    /// Handler for unknown routes.
    /// </summary>
    /// <param name="userData">User provided custom data.</param>
    /// <param name="reason">Reason that caused the routing here for instance <see cref="HttpStatusCode.NotFound"/> or <see cref="HttpStatusCode.MethodNotAllowed"/></param>
    /// <returns>The response object.</returns>
    public delegate object? DefaultRequestHandler(object? userData, HttpStatusCode reason);

    /// <summary>
    /// Handler for exceptions.
    /// </summary>
    /// <param name="userData">User provided custom data.</param>
    /// <param name="exception">Exception that was being caught.</param>
    /// <returns>The response object.</returns>
    public delegate object? ExceptionHandler<TException>(object? userData, TException exception) where TException: Exception;

    /// <summary>
    /// Handler for exceptions.
    /// </summary>
    /// <param name="userData">User provided custom data.</param>
    /// <param name="exception">Exception that was being caught.</param>
    /// <returns>The response object.</returns>
    public delegate TResult ExceptionHandler<TException, TResult>(object? userData, TException exception) where TException : Exception;

    /// <summary>
    /// Handler for unknown routes.
    /// </summary>
    /// <param name="userData">User provided custom data.</param>
    /// <param name="reason">Reason that caused the routing here for instance <see cref="HttpStatusCode.NotFound"/> or <see cref="HttpStatusCode.MethodNotAllowed"/></param>
    /// <returns>The response object.</returns>
    public delegate TResult DefaultRequestHandler<TResult>(object? userData, HttpStatusCode reason);

    /// <summary>
    /// Handler for known routes.
    /// </summary>
    /// <param name="paramz">The request parameters.</param>
    /// <param name="userData">User provided custom data.</param>
    /// <returns>The response object.</returns>
    public delegate object? RequestHandler(IReadOnlyDictionary<string, object?> paramz, object? userData);

    /// <summary>
    /// Handler for known routes.
    /// </summary>
    /// <param name="paramz">The request parameters.</param>
    /// <param name="userData">User provided custom data.</param>
    /// <returns>The response object.</returns>
    public delegate TResult RequestHandler<TResult>(IReadOnlyDictionary<string, object?> paramz, object? userData);

    /// <summary>
    /// Router delegate.
    /// </summary>
    /// <param name="userData">Custom user data to be passed to handlers.</param>
    /// <param name="path">Path where to route the request. Must NOT include the base URL.</param>
    /// <param name="method">Method(s) to be accepted.</param>
    /// <param name="splitOptions">Specifies how to split the <paramref name="path"/>.</param>
    /// <returns>The response object.</returns>
    public delegate object? Router(object? userData, ReadOnlySpan<char> path, ReadOnlySpan<char> method, SplitOptions? splitOptions = null);

    /// <summary>
    /// Async router delegate.
    /// </summary>
    /// <param name="userData">Custom user data to be passed to handlers.</param>
    /// <param name="path">Path where to route the request. Must NOT include the base URL.</param>
    /// <param name="method">Method(s) to be accepted.</param>
    /// <param name="splitOptions">Specifies how to split the <paramref name="path"/>.</param>
    /// <returns>The response object.</returns>
    public delegate Task<object?> AsyncRouter(object? userData, ReadOnlySpan<char> path, ReadOnlySpan<char> method, SplitOptions? splitOptions = null);
}
