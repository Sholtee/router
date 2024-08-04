/********************************************************************************
* MsDiRequestHandlerBuilder.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Router.Extensions
{
    using Primitives;

    /// <summary>
    /// Helper class to build <see cref="IServiceProvider"/> backed service handlers.
    /// </summary>
    public class MsDiRequestHandlerBuilder : RequestHandlerBuilder
    {
        /// <inheritdoc/>
        protected override MethodInfo CreateServiceMethod { get; } = MethodInfoExtractor.Extract<IServiceProvider>(static sp => sp.GetService(null));

        /// <inheritdoc/>
        #if DEBUG
        internal
        #endif
        protected override Expression GetCreateServiceArgument(ParameterInfo param, Type serviceType, object? userData) => param.Position switch
        {
            0 => Expression.Constant(serviceType),
            _ => throw new NotSupportedException()
        };
    }
}
