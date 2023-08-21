/********************************************************************************
* UnfoldLambdaExpressionVisitor.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.Router.Internals
{
    internal sealed class UnfoldLambdaExpressionVisitor : ExpressionVisitor
    {
        private IReadOnlyList<ParameterExpression>? FParameters;

        private UnfoldLambdaExpressionVisitor(IReadOnlyList<Expression> parameterSubstitutions)
            => ParameterSubstitutions = parameterSubstitutions;

        public IReadOnlyList<Expression> ParameterSubstitutions { get; }

        public static Expression Unfold(LambdaExpression lamda, params Expression[] parameterSubstitutions) =>
            new UnfoldLambdaExpressionVisitor(parameterSubstitutions).Visit(lamda);

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (FParameters is not null)
                //
                // In nested lambdas we just replace the captured compatible variables.
                //

                return base.VisitLambda(node);

            Debug.Assert
            (
                node.Parameters.Count == ParameterSubstitutions.Count,
                "ParameterSubstitutions must provide as many parameters as the lambda function has"
            );

            FParameters = node.Parameters;

            //
            // From the main method we need the method body only
            //

            return Visit(node.Body);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            //
            // Find the corresponding parameter
            //

            int? index = FParameters
                .Select(static (p, i) => new { Parameter = p, Index = i })
                .SingleOrDefault(p => p.Parameter == node)
                ?.Index;

            return index is not null
                ? ParameterSubstitutions[index.Value]
                : node;
        }
    }
}
