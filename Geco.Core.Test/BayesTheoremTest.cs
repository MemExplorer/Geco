using System.Diagnostics;

namespace Geco.Core.Test;

public class BayesTheoremTest
{
	[Fact]
	void StabilityTest()
	{
		var bayesInst = new BayesTheorem();
		bayesInst.AppendData("Charging", 2, 7);
		bayesInst.AppendData("Usage", 8, 12);
		bayesInst.AppendData("Network", 6, 10);
		var computation = bayesInst.Compute();
		Debug.Assert(computation.isPositive == false);
	}
}
