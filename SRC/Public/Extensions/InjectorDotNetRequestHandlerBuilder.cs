/********************************************************************************
* InjectorDotNetRequestHandlerBuilder.cs                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Router.Extensions
{
    using DI.Interfaces;
    using Primitives;

    /// <summary>
    /// Helper class to build <see cref="IInjector"/> backed service handlers.
    /// </summary>
    public class InjectorDotNetRequestHandlerBuilder : RequestHandlerBuilder
    {
        /// <inheritdoc/>
        protected override MethodInfo CreateServiceMethod { get; } = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null));

        /// <inheritdoc/>
        #if DEBUG
        internal
        #endif
        protected override Expression GetCreateServiceArgument(ParameterInfo param, Type serviceType, object? userData) => param.Position switch
        {
            0 => Expression.Constant(serviceType),
            1 => Expression.Constant(null, typeof(string)),
            _ => throw new NotSupportedException()
        };
    }
}
