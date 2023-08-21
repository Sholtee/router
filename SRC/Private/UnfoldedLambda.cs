/********************************************************************************
* UnfoldedLambda.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Solti.Utils.Router.Internals
{
    internal sealed class UnfoldedLambda : ExpressionVisitor
    {
        private ReadOnlyCollection<ParameterExpression>? FParameters;

        private readonly Expression[] FParameterSubstitutions;

        private UnfoldedLambda(Expression[] parameterSubstitutions)
            => FParameterSubstitutions = parameterSubstitutions;

        public static Expression Create(LambdaExpression lamda, params Expression[] parameterSubstitutions)
            => new UnfoldedLambda(parameterSubstitutions).Visit(lamda);

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            if (FParameters is not null)
                //
                // In nested lambdas we just replace the captured compatible variables.
                //

                return base.VisitLambda(lambda);

            Debug.Assert
            (
                lambda.Parameters.Count == FParameterSubstitutions.Length,
                "ParameterSubstitutions must provide as many parameters as the lambda function has"
            );

            FParameters = lambda.Parameters;

            //
            // From the main method we need the method body only
            //

            return Visit(lambda.Body);
        }

        protected override Expression VisitParameter(ParameterExpression parameter)
        {
            //
            // Find the corresponding parameter
            //

            int index = FParameters!.IndexOf(parameter);
            return index >= 0 ? FParameterSubstitutions[index] : parameter;
        }
    }
}
