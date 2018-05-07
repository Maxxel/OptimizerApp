using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace LambdaOptimizer
{
    public class Helper
    {
	    public delegate TOut Function<in TIn, out TOut>(TIn[] delParams);

	    public static TOut OptimizedCalculation<TIn, TOut, TFout>(Expression<Func<TIn[], TOut>> lambdaExpression, TIn[] lambdaParams, Function<TIn, TFout> f)
	    {
		    var expression = lambdaExpression.Body;
		    var paramStack = new List<CustomParams<TIn, TFout>>();

		    RecursiveOptimization(ref expression, ref paramStack, f, lambdaParams);

		    paramStack.ForEach(i => i.Result = i.OptimizedFunction.Invoke());

		    var result = Expression.Lambda(expression);

		    return (TOut)result.Compile().DynamicInvoke();
	    }

		private static void CreateExpressionByNodeType(ref Expression expr, ExpressionType type, Expression leftSide, Expression rightSide, Expression additional = null)
		{
			switch (type)
			{
				case ExpressionType.Modulo:
					expr = Expression.Modulo(leftSide, rightSide);
					break;
				case ExpressionType.Divide:
					expr = Expression.Divide(leftSide, rightSide);
					break;
				case ExpressionType.Multiply:
					expr = Expression.Multiply(leftSide, rightSide);
					break;
				case ExpressionType.Subtract:
					expr = Expression.Subtract(leftSide, rightSide);
					break;
				case ExpressionType.Add:
					expr = Expression.Add(leftSide, rightSide);
					break;
				case ExpressionType.LessThan:
					expr = Expression.LessThan(leftSide, rightSide);
					break;
				case ExpressionType.LessThanOrEqual:
					expr = Expression.LessThanOrEqual(leftSide, rightSide);
					break;
				case ExpressionType.GreaterThan:
					expr = Expression.GreaterThan(leftSide, rightSide);
					break;
				case ExpressionType.GreaterThanOrEqual:
					expr = Expression.GreaterThanOrEqual(leftSide, rightSide);
					break;
				case ExpressionType.Conditional:
					expr = Expression.Condition(additional ?? throw new ArgumentNullException(nameof(additional)), leftSide, rightSide);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, "Operation type not defined.");
			}
		}

		private static void RecursiveOptimization<TIn, TFout>(ref Expression expr, ref List<CustomParams<TIn, TFout>> paramStack, Function<TIn, TFout> f, TIn[] lambdaParams)
		{
			if (expr is ConditionalExpression conditional)
			{

				var isTrue = conditional.IfTrue;
				var isFalse = conditional.IfFalse;
				var conditionTest = conditional.Test;

				RecursiveOptimization(ref isTrue, ref paramStack, f, lambdaParams);
				RecursiveOptimization(ref isFalse, ref paramStack, f, lambdaParams);
				RecursiveOptimization(ref conditionTest, ref paramStack, f, lambdaParams);

				CreateExpressionByNodeType(ref expr, conditional.NodeType, isTrue, isFalse, conditionTest);
			}

			else if (expr is BinaryExpression binary)
			{
				var left = binary.Left;
				var right = binary.Right;

				RecursiveOptimization(ref left, ref paramStack, f, lambdaParams);
				RecursiveOptimization(ref right, ref paramStack, f, lambdaParams);

				CreateExpressionByNodeType(ref expr, binary.NodeType, left, right);

			}
			else if (expr is InvocationExpression method)
			{
				var args = method.Arguments[0];
				var newArrayExpression =
					Expression.NewArrayInit(typeof(TIn[]), args).Expressions[0].ToString();
				var arrayIndexes =
					Regex.Matches(newArrayExpression, @"array\[(.*?)\]").
					Cast<Match>().Select(m => Convert.ToInt32(m.Groups[1].Value)).
					ToArray();

				var newArray = GetNewArray<TIn>(arrayIndexes, lambdaParams);

				CustomParams<TIn, TFout> customParam;
				if (paramStack.Any(i => i.Array.SequenceEqual(newArray)))
				{
					customParam = paramStack.First(i => i.Array.SequenceEqual(newArray));
				}
				else
				{
					customParam = new CustomParams<TIn, TFout>
					{
						Array = newArray
					};
					customParam.OptimizedFunction = () => f(customParam.Array);
					paramStack.Add(customParam);
				}
				Expression<Func<TFout>> opt = () => customParam.Result;
				expr = Expression.Invoke(opt);
			}
		}

		private static T[] GetNewArray<T>(IReadOnlyCollection<int> indexes, IReadOnlyList<T> oldArray)
		{
			var newArray = new T[indexes.Count];
			var id = 0;
			foreach (var index in indexes)
			{
				newArray[id++] = oldArray[index];
			}

			return newArray;
		}
	}
}
