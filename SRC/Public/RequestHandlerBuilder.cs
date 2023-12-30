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

namespace Solti.Utils.Router
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
    ///     return ((IserviceProvider) userData).GetService(typeof(IMyService)).MyHandler((TArg1) paramz["arg1"], (TArg2) paramz["arg2"]);
    /// }
    /// </code>
    /// </remarks>
    public class RequestHandlerBuilder
    {
        private static readonly MethodInfo
            FGetService = MethodInfoExtractor.Extract<IServiceProvider>(static sp => sp.GetService(null)),
            FGetParam   = MethodInfoExtractor.Extract<IReadOnlyDictionary<string, object?>, object?>(static dict => dict[null!]);

        /// <summary>
        /// Expression reflecting the "userData" parameter of <see cref="RequestHandler{TResult}.Invoke(IReadOnlyDictionary{string, object?}, object?)"/>
        /// </summary>
        protected ParameterExpression UserData { get; } = Expression.Parameter(typeof(object), "userData");

        /// <summary>
        /// Expression reflecting the "paramz" parameter of <see cref="RequestHandler{TResult}.Invoke(IReadOnlyDictionary{string, object?}, object?)"/>
        /// </summary>
        protected ParameterExpression ParamsDict { get; } = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), "paramz");

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
        protected virtual Expression CreateService(object? userData) => Expression.Convert
        (
            Expression.Call
            (
                Expression.Convert
                (
                    UserData,
                    CreateServiceMethod.ReflectedType
                ),
                CreateServiceMethod,
                CreateServiceMethod.GetParameters().Select(arg => GetCreateServiceArgument(arg, userData))
            ),
            InvokeServiceMethod.ReflectedType
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
        protected virtual Expression GetCreateServiceArgument(ParameterInfo param, object? userData)
        {
            if (param is null)
                throw new ArgumentNullException(nameof(param));

            return param.Position switch
            {
                0 => Expression.Constant(InvokeServiceMethod.ReflectedType),
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        /// The concrete method to handle the request.
        /// </summary>
        public MethodInfo InvokeServiceMethod { get; }

        /// <summary>
        /// Gets the argument name to be used when getting the value from the "paramz" dict.
        /// </summary>
        /// <remarks>Override this method to introduce alias support.</remarks>
        protected virtual string GetArgumentName(ParameterInfo arg) => arg.Name;

        /// <summary>
        /// Returns the argument(s) to be passed to the <see cref="InvokeServiceMethod"/>.
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected virtual Expression GetInvokeServiceArgument(ParameterInfo param, ParsedRoute route, object? userData)
        {
            if (param is null)
                throw new ArgumentNullException(nameof(param));

            if (param.ParameterType.IsByRef)
                throw new ArgumentException(BY_REF_PARAMETER, param.Name);

            if (route is null)
                throw new ArgumentNullException(nameof(route));

            string name = GetArgumentName(param);

            if (!route.Parameters.ContainsKey(name))
            {
                ArgumentException ex = new(PARAM_NOT_DEFINED, param.Name);
                ex.Data["actualName"] = name;
                throw ex;
            }

            return Expression.Convert
            (
                Expression.Call
                (
                    ParamsDict,
                    FGetParam,
                    Expression.Constant(name)
                ),
                param.ParameterType
            );
        }

        /// <summary>
        /// Invokes the concrete request handler
        /// </summary>
        /// <remarks>
        /// <code>
        /// MyHandler((TArg1) paramz["arg1"], (TArg2) paramz["arg2"])
        /// </code>
        /// </remarks>
        #if DEBUG
        internal
        #endif
        protected virtual Expression InvokeService(ParsedRoute route, object? userData)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            Expression call = Expression.Call
            (
                CreateService(userData),
                InvokeServiceMethod,
                InvokeServiceMethod.GetParameters().Select(arg => GetInvokeServiceArgument(arg, route, userData))
            );
            return InvokeServiceMethod.ReturnType != typeof(void)
                ? call
                : Expression.Block
                (
                   type: typeof(object),
                   call,
                   Expression.Constant(null, typeof(object))
                );
        }

        /// <summary>
        /// Creates a new <see cref="RequestHandlerBuilder"/> method.
        /// </summary>
        public RequestHandlerBuilder(MethodInfo invokeServiceMethod) => InvokeServiceMethod = invokeServiceMethod ?? throw new ArgumentNullException(nameof(invokeServiceMethod));

        /// <summary>
        /// Creates the <see cref="LambdaExpression"/> that represents the service invocation. The returned lambda is safe to be passed to <see cref="AsyncRouterBuilder.AddRoute(ParsedRoute, LambdaExpression, string[])"/> method.
        /// </summary>
        /// <remarks>
        /// <code>
        /// (IReadOnlyDictionary&lt;string, object?&gt; paramz, object? userData) =>
        /// {
        ///     return ((IserviceProvider) userData).GetService(typeof(IMyService)).MyHandler((TArg1) paramz["arg1"], (TArg2) paramz["arg2"]);
        /// }
        /// </code>
        /// </remarks>
        public virtual LambdaExpression CreateLambda(ParsedRoute route, object? userData)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            Type lambdaType = typeof(RequestHandler<>).MakeGenericType
            (
                InvokeServiceMethod.ReturnType != typeof(void)
                    ? InvokeServiceMethod.ReturnType
                    : typeof(object)
            );

            LambdaExpression lambda = Expression.Lambda(lambdaType, InvokeService(route, userData), ParamsDict, UserData);
            Debug.WriteLine(lambda.GetDebugView());
            return lambda;
        }
    }
}
