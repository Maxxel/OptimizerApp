using System;

namespace LambdaOptimizer
{
	internal class CustomParams<TIn,TOut>
	{
		public TIn[] Array { get; set; }				

		public Func<TOut> OptimizedFunction { get; set; }

		public TOut Result { get; set; }
	}
}
