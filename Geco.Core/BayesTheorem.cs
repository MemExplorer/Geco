namespace Geco.Core;

public record BayesTheoremAttribute(int Positive, int Negative);

public class BayesTheorem
{
	readonly Dictionary<string, BayesTheoremAttribute> _frequencyTbl = new();

	public void AppendData(string attrName, int positive, int negative) =>
		_frequencyTbl.Add(attrName, new BayesTheoremAttribute(positive, negative));

	public (bool isPositive, double prob) Compute()
	{
		double totalPositiveAttr = _frequencyTbl.Values.Sum(attr => attr.Positive);
		double totalNegativeAttr = _frequencyTbl.Values.Sum(attr => attr.Negative);
		double sumTblFrequency = totalPositiveAttr + totalNegativeAttr;

		double positivePosterior = _frequencyTbl.Values.Aggregate(1.0, (a, x) =>
		{
			double attrTotalFreq = x.Positive + x.Negative;
			return a * (x.Positive / attrTotalFreq);
		}) * (totalPositiveAttr / sumTblFrequency);

		double negativePosterior = _frequencyTbl.Values.Aggregate(1.0, (a, x) =>
		{
			double attrTotalFreq = x.Positive + x.Negative;
			return a * (x.Negative / attrTotalFreq);
		}) * (totalNegativeAttr / sumTblFrequency);


		// proportional probability
		double posteriorSum = positivePosterior + negativePosterior;
		double proportionalPositiveProb = positivePosterior / posteriorSum;
		double proportionalNegativeProb = negativePosterior / posteriorSum;

		return positivePosterior > negativePosterior
			? (true, proportionalPositiveProb)
			: (false, proportionalNegativeProb);
	}
}
