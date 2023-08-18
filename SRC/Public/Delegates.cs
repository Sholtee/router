﻿/********************************************************************************
* Delegates.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Router
{
    /// <summary>
    /// Creates <see cref="TryConvert"/> instances.
    /// </summary>
    /// <param name="userData">User provided data, comes from the route template.</param>
    public delegate TryConvert ConverterFactory(string? userData);

    /// <summary>
    /// Tries to convert the input string to a given type.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <param name="value">The converted value if the conversion was successful</param>
    public delegate bool TryConvert(string input, out object? value);

    /// <summary>
    /// Handler for unknown routes.
    /// </summary>
    /// <param name="userData">User provided custom data.</param>
    /// <param name="path">Path led to here</param>
    /// <returns>The response object.</returns>
    public delegate object DefaultRequestHandler(object? userData, string path);

    /// <summary>
    /// Handler for known routes.
    /// </summary>
    /// <param name="paramz">The request parameters.</param>
    /// <param name="userData">User provided custom data.</param>
    /// <param name="path">Path led to here</param>
    /// <returns>The response object.</returns>
    public delegate object? RequestHandler(IReadOnlyDictionary<string, object?> paramz, object? userData, string path);

    /// <summary>
    /// Router delegate.
    /// </summary>
    /// <param name="userData">Custom user data to be passed to handlers.</param>
    /// <param name="path">Path where to route the request.</param>
    /// <returns>The response object.</returns>
    public delegate object? Router(object? userData, string path);
}
