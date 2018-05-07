using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using LambdaOptimizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LambdaOptimizerTest
{
	[TestClass]
	public class OptimizerTestClass
	{
		[TestMethod]
		public void TestConditionalWithInt()
		{
			int[] testArray = { 1, 18, 0, -5, 12, 21 };
			var function = new Helper.Function<int, float>(TestFunctionInt);
			Expression<Func<int[], bool>> lambdaExpression = array =>   
										(function(new[] { array[0], array[2], array[5] }) > function(new[] { array[1], array[2], array[3] })) ? 
										(function(new[] { array[0], array[2], array[5] }) < 120) : 
													(function(new[] { array[1], array[2], array[3] }) + 15 > 50) ? 
															true : 
															false;
			var resWithoutOptim = lambdaExpression.Compile().Invoke(testArray);
			var resWithOptim = Helper.OptimizedCalculation(lambdaExpression, testArray, function);

			Assert.AreEqual(resWithOptim, resWithoutOptim, string.Format(AreEqualError,resWithOptim,resWithoutOptim));
		}

		[TestMethod]
		public void TestBinaryWithInt()
		{
			int[] testArray = { 1, 18, 0, -5, 12, 21 };
			var function = new Helper.Function<int, float>(TestFunctionInt);
			Expression<Func<int[], float>> lambdaExpression = array =>
											(function(new[] { array[0], array[2], array[5] }) + function(new[] { array[1], array[2], array[3] })) -
											 function(new[] { array[0], array[2], array[5] });
			var resWithoutOptim = lambdaExpression.Compile().Invoke(testArray);
			var resWithOptim = Helper.OptimizedCalculation(lambdaExpression, testArray, function);

			Assert.AreEqual(resWithOptim, resWithoutOptim, string.Format(AreEqualError, resWithOptim, resWithoutOptim));
		}

		[TestMethod]
		public void TestConditionalWithString()
		{
			string[] testArray = { "aa", "bb", "cc", "dd", "ee", "ff" };
			var function = new Helper.Function<string, int>(TestFunctionString);
			Expression<Func<string[], bool>> lambdaExpression = array =>
				(function(new[] {array[0], array[2]}) < function(new[] {array[0], array[1]})) ?
				(function(new[] { array[0], array[2], array[4], array[4] }) > (function(new[] { array[0], array[2] }))) :
				(function(new[] { array[0], array[2] }) <= (function(new[] { array[1], array[3], array[3] }))) ;																		
			var resWithoutOptim = lambdaExpression.Compile().Invoke(testArray);
			var resWithOptim = Helper.OptimizedCalculation(lambdaExpression, testArray, function);

			Assert.AreEqual(resWithOptim, resWithoutOptim, string.Format(AreEqualError, resWithOptim, resWithoutOptim));
		}

		[TestMethod]
		public void TestPerformance()
		{
			int[] testArray = { 1, 3, 8, 4 };
			var function = new Helper.Function<int, long>(PerformanceFunction);
			Expression<Func<int[], long>> lambdaExpression = array =>
				(function(new[] { array[0], array[2], array[3] }) > function(new[] { array[0], array[2] })) ?
					(function(new[] { array[0], array[2], array[1], array[1] }) + (function(new[] { array[0], array[2], array[3] }))) *
					(function(new[] { array[0], array[2], array[1], array[1] }) + (function(new[] { array[0], array[2], array[3] }))) +
					(function(new[] { array[0], array[2], array[1], array[1] }) + (function(new[] { array[0], array[2], array[3] }))) +
					(function(new[] { array[0], array[2], array[1], array[1] }) + (function(new[] { array[0], array[2], array[3] }))) :
					(function(new[] { array[0], array[2] }) - (function(new[] { array[0], array[2], array[3] }))) * 3;

			var stopwatch = Stopwatch.StartNew();
			var resWithoutOptim = lambdaExpression.Compile().Invoke(testArray);
			stopwatch.Stop();
			var time1 = stopwatch.Elapsed.Seconds;

			stopwatch = Stopwatch.StartNew();
			var resWithOptim = Helper.OptimizedCalculation(lambdaExpression, testArray, function);
			stopwatch.Stop();
			var time2 = stopwatch.Elapsed.Seconds;


			Assert.IsTrue(time2 < time1, string.Format( PerformError, 
														"timer2 : " + resWithOptim,
														"timer1 : " + resWithoutOptim));
		}



		private static long PerformanceFunction(int[] functionParams)
		{
			Thread.Sleep(2000);
			long Factorial(long x) => x == 0 ? 1 : x * Factorial(x - 1);

			return functionParams.Sum(param=> Factorial(param));
		}		

		private static float TestFunctionInt(int[] functionParams)
		{
			return functionParams.Sum(param => (float)param / 2);
		}

		private static int TestFunctionString(string[] functionParams)
		{
			var result = 0;
			foreach (var param in functionParams)
			{
				switch (param)
				{
					case "aa":
						result += 2;						
						break;
					case "bb":
						result += 5;
						break;
					default:
						result += 3;
						break;
				}
			}
			return result;
		}

		private const string AreEqualError = @"Error. Expect {0} == {1}";

		private const string PerformError = @"Error. Expect {0} < {1}";
	}
}
