/********************************************************************************
* RequestHandlerBuilder.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Router.Extensions
{
    using Primitives;

    using static Properties.Resources;

    /// <summary>
    /// Helper class to build IOC backed service handlers.
    /// </summary>
    /// <remarks>
    /// <code>
    /// (IReadOnlyDictionary&lt;string, object?&gt; paramz, object? userData) =>
    /// {
    ///     IElementAccessByInternalId elementAccess = (IElementAccessByInternalId) paramz;
    ///     return ((IserviceProvider) userData).GetService(typeof(IMyService)).MyHandler((TArg1) elementAccess[internalId_1], (TArg2) elementAccess[internalId_2]);
    /// }
    /// </code>
    /// </remarks>
    public class RequestHandlerBuilder
    {
        private static readonly MethodInfo
            FGetService = MethodInfoExtractor.Extract<IServiceProvider>(static sp => sp.GetService(null)),
            FGetParam   = MethodInfoExtractor.Extract<IParamAccessByInternalId<object?>, object?>(static dict => dict[0]);

        /// <summary>
        /// Expression reflecting the "userData" parameter of <see cref="RequestHandler{TResult}.Invoke(IReadOnlyDictionary{string, object?}, object?)"/>
        /// </summary>
        protected static ParameterExpression UserData { get; } = Expression.Parameter(typeof(object), "userData");

        /// <summary>
        /// Expression reflecting the "paramz" parameter of <see cref="RequestHandler{TResult}.Invoke(IReadOnlyDictionary{string, object?}, object?)"/>
        /// </summary>
        protected static ParameterExpression ParamsDict { get; } = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), "paramz");

        /// <summary>
        /// Expression reflecting the <see cref="IParamAccessByInternalId{TData}"/> variable.
        /// </summary>
        protected static ParameterExpression ElementAccess { get; } = Expression.Variable(typeof(IParamAccessByInternalId<object?>), "elementAccess");

        /// <summary>
        /// Specifies how to create the service instance. The default implementation uses the <see cref="IServiceProvider"/> interface.
        /// </summary>
        /// <remarks>
        /// <code>
        /// ((IServiceProvider) userData).GetService(typeof(IMyService))
        /// </code>
        /// </remarks>
        #if DEBUG
        internal
        #endif
        protected virtual Expression CreateService(Type serviceType, object? userData) => Expression.Convert
        (
            Expression.Call
            (
                Expression.Convert
                (
                    UserData,
                    CreateServiceMethod.ReflectedType
                ),
                CreateServiceMethod,
                CreateServiceMethod.GetParameters().Select(arg => GetCreateServiceArgument(arg, serviceType, userData))
            ),
            serviceType
        );

        /// <summary>
        /// The concrete method being invoked to create the particular service instance. By default it points to the <see cref="IServiceProvider.GetService(Type)"/> method.
        /// </summary>
        protected virtual MethodInfo CreateServiceMethod { get; } = FGetService;

        /// <summary>
        /// Returns the argument(s) to be passed to the <see cref="CreateServiceMethod"/>.
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected virtual Expression GetCreateServiceArgument(ParameterInfo param, Type serviceType, object? userData) => param.Position switch
        {
            0 => Expression.Constant(serviceType),
            _ => throw new NotSupportedException()
        };

        /// <summary>
        /// Gets the argument name to be used when getting the value from the "paramz" dict.
        /// </summary>
        /// <remarks>Override this method to introduce alias support.</remarks>
        protected virtual string GetArgumentName(ParameterInfo arg) => arg.Name;

        /// <summary>
        /// Returns the argument(s) to be passed to the request handler.
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected virtual Expression GetInvokeServiceArgument(ParameterInfo param, ParsedRoute route, IReadOnlyDictionary<string, int> shortcuts, object? userData)
        {
            if (param.ParameterType.IsByRef)
                throw new ArgumentException(BY_REF_PARAMETER, param.Name);

            string name = GetArgumentName(param);

            if (!route.Parameters.TryGetValue(name, out Type paramType))
            {
                ArgumentException ex = new(PARAM_NOT_DEFINED, param.Name);
                ex.Data["actualName"] = name;
                throw ex;
            }

            if (!param.ParameterType.IsAssignableFrom(paramType))
            {
                ArgumentException ex = new(PARAM_TYPE_NOT_COMPATIBLE, param.Name);
                ex.Data["actualName"] = name;
                ex.Data["requiredType"] = param.ParameterType;
                throw ex;
            }

            if (!shortcuts.TryGetValue(name, out int id))
            {
                ArgumentException ex = new(NO_SHORTCUT, param.Name);
                throw ex;
            }

            return Expression.Convert
            (
                Expression.Call
                (
                    ElementAccess,
                    FGetParam,
                    Expression.Constant(id)
                ),
                param.ParameterType
            );
        }

        /// <summary>
        /// Invokes the concrete request handler
        /// </summary>
        /// <remarks>
        /// <code>
        /// IElementAccessByInternalId elementAccess = (IElementAccessByInternalId) paramz;
        /// MyHandler((TArg1) elementAccess[internalId_1], (TArg2) elementAccess[internalId_2])
        /// </code>
        /// </remarks>
#if DEBUG
        internal
#endif
        protected virtual Expression InvokeService(ParsedRoute route, MethodInfo invokeServiceMethod, IReadOnlyDictionary<string, int> shortcuts, object? userData)
        {
            List<Expression> block =
            [
                Expression.Assign
                (
                    ElementAccess,
                    Expression.Convert
                    (
                        ParamsDict,
                        typeof(IParamAccessByInternalId<object?>)
                    )
                ),
                Expression.Call
                (
                    CreateService(invokeServiceMethod.ReflectedType, userData),
                    invokeServiceMethod,
                    invokeServiceMethod
                        .GetParameters()
                        .Select(arg => GetInvokeServiceArgument(arg, route, shortcuts, userData))
                )
            ];

            Type blockType;

            if (invokeServiceMethod.ReturnType == typeof(void))
            {
                blockType = typeof(object);
                block.Add
                (
                    Expression.Constant(null, typeof(object))
                );
            }
            else blockType = invokeServiceMethod.ReturnType;

            return Expression.Block
            (
                type: blockType,
                variables: new ParameterExpression[] { ElementAccess },
                block
            );
        }

        /// <summary>
        /// Creates the <see cref="LambdaExpression"/> that represents the service invocation. The returned lambda is safe to be passed to <see cref="AsyncRouterBuilder.AddRoute(ParsedRoute, LambdaExpression, string[])"/> method.
        /// </summary>
        /// <remarks>
        /// <code>
        /// (IReadOnlyDictionary&lt;string, object?&gt; paramz, object? userData) =>
        /// {
        ///     IElementAccessByInternalId elementAccess = (IElementAccessByInternalId) paramz;
        ///     return ((IserviceProvider) userData).GetService(typeof(IMyService)).MyHandler((TArg1) elementAccess[internalId_1], (TArg2) elementAccess[internalId_2]);
        /// }
        /// </code>
        /// </remarks>
        public virtual UntypedRequestHandlerFactory CreateFactory(MethodInfo invokeServiceMethod, object? userData)
        {
            if (invokeServiceMethod is null)
                throw new ArgumentNullException(nameof(invokeServiceMethod));

            if (invokeServiceMethod.IsGenericMethodDefinition || invokeServiceMethod.IsStatic || (!invokeServiceMethod.ReflectedType.IsInterface && invokeServiceMethod.IsAbstract))
                throw new ArgumentException(INVALID_HANDLER, nameof(invokeServiceMethod));

            return (route, shortcuts) =>
            {
                LambdaExpression lambda = Expression.Lambda
                (
                    typeof(RequestHandler<>).MakeGenericType
                    (
                        invokeServiceMethod.ReturnType != typeof(void)
                            ? invokeServiceMethod.ReturnType
                            : typeof(object)
                    ),
                    InvokeService(route, invokeServiceMethod, shortcuts, userData),
                    ParamsDict,
                    UserData
                );
                Debug.WriteLine(lambda.GetDebugView());

                return lambda;
            };
        }
    }
}
