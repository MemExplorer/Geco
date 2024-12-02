namespace Geco.Core;

public record BayesTheoremAttribute(int Positive, int Negative);
public class BayesTheorem
{
	Dictionary<string, BayesTheoremAttribute> _frequencyTbl = new Dictionary<string, BayesTheoremAttribute>();

	public void AppendData(string attrName, int positive, int negative) =>
		_frequencyTbl.Add(attrName, new(positive, negative));

	public (bool isPositive, double prob) Compute()
	{
		double totalPositiveAttr = _frequencyTbl.Values.Sum(attr => attr.Positive);
		double totalNegativeAttr = _frequencyTbl.Values.Sum(attr => attr.Negative);
		double sumTblFrequency = totalPositiveAttr + totalNegativeAttr;

		var positivePoterior = _frequencyTbl.Values.Aggregate(1.0, (a, x) =>
		{
			double attrTotalFreq = x.Positive + x.Negative;
			return a * (x.Positive / attrTotalFreq);
		}) * (totalPositiveAttr / sumTblFrequency);

		var negativePosterior = _frequencyTbl.Values.Aggregate(1.0, (a, x) =>
		{
			double attrTotalFreq = x.Positive + x.Negative;
			return a * (x.Negative / attrTotalFreq);
		}) * (totalNegativeAttr / sumTblFrequency);


		// proportional probability
		var posteriorSum = positivePoterior + negativePosterior;
		var proportionalPositiveProb = positivePoterior / posteriorSum;
		var proportionalNegativeProb = negativePosterior / posteriorSum;

		if (positivePoterior > negativePosterior)
			return (true, proportionalPositiveProb);

		return (false, proportionalNegativeProb);
	}
}
