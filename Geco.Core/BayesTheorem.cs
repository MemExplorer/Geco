using System.Text;

namespace Geco.Core;

public record BayesTheoremAttribute(int Positive, int Negative);
public record BayesComputationResult(double PositiveProbability, double NegativeProbability);

public class BayesTheorem
{
	const double Alpha = 0.1;
	readonly Dictionary<string, BayesTheoremAttribute> _frequencyTbl = new();
	private bool _needSmoothing;

	public IDictionary<string, BayesTheoremAttribute> GetFrequencyData() => _frequencyTbl;

	public void AppendData(string attrName, int positive, int negative)
	{
		if ((positive == 0 || negative == 0) && !_needSmoothing)
			_needSmoothing = true;

		_frequencyTbl.Add(attrName, new BayesTheoremAttribute(positive, negative));
	}

	public (string PositiveComputation, string NegativeComputation) GetComputationSolution()
	{
		double totalPositiveAttr = _frequencyTbl.Values.Sum(attr => attr.Positive);
		double totalNegativeAttr = _frequencyTbl.Values.Sum(attr => attr.Negative);
		double sumTblFrequency = totalPositiveAttr + totalNegativeAttr;
		int kValue = _frequencyTbl.Count;

		var positiveComputation = new StringBuilder();
		var negativeComputation = new StringBuilder();
		foreach (var x in _frequencyTbl.Values)
		{
			double attrTotalFreq = x.Positive + x.Negative;
			if (_needSmoothing)
			{
				positiveComputation.Append("((");
				positiveComputation.Append(x.Positive);
				positiveComputation.Append('+');
				positiveComputation.Append(Alpha);
				positiveComputation.Append(")/(");
				positiveComputation.Append(attrTotalFreq);
				positiveComputation.Append('+');
				positiveComputation.Append(Alpha);
				positiveComputation.Append('*');
				positiveComputation.Append(kValue);
				positiveComputation.Append(")) * ");

				negativeComputation.Append("((");
				negativeComputation.Append(x.Negative);
				negativeComputation.Append('+');
				negativeComputation.Append(Alpha);
				negativeComputation.Append(")/(");
				negativeComputation.Append(attrTotalFreq);
				negativeComputation.Append('+');
				negativeComputation.Append(Alpha);
				negativeComputation.Append('*');
				negativeComputation.Append(kValue);
				negativeComputation.Append(")) * ");
			}
			else
			{
				positiveComputation.Append('(');
				positiveComputation.Append(x.Positive);
				positiveComputation.Append('/');
				positiveComputation.Append(attrTotalFreq);
				positiveComputation.Append(") * ");

				negativeComputation.Append('(');
				negativeComputation.Append(x.Negative);
				negativeComputation.Append('/');
				negativeComputation.Append(attrTotalFreq);
				negativeComputation.Append(") * ");
			}
		}

		if (_needSmoothing)
		{
			positiveComputation.Append("((");
			positiveComputation.Append(totalPositiveAttr);
			positiveComputation.Append('+');
			positiveComputation.Append(Alpha);
			positiveComputation.Append(")/(");
			positiveComputation.Append(sumTblFrequency);
			positiveComputation.Append('+');
			positiveComputation.Append(Alpha);
			positiveComputation.Append('*');
			positiveComputation.Append(kValue);
			positiveComputation.Append("))");

			negativeComputation.Append("((");
			negativeComputation.Append(totalNegativeAttr);
			negativeComputation.Append('+');
			negativeComputation.Append(Alpha);
			negativeComputation.Append(")/(");
			negativeComputation.Append(sumTblFrequency);
			negativeComputation.Append('+');
			negativeComputation.Append(Alpha);
			negativeComputation.Append('*');
			negativeComputation.Append(kValue);
			negativeComputation.Append("))");
		}
		else
		{
			positiveComputation.Append('(');
			positiveComputation.Append(totalPositiveAttr);
			positiveComputation.Append('/');
			positiveComputation.Append(sumTblFrequency);
			positiveComputation.Append(')');

			negativeComputation.Append('(');
			negativeComputation.Append(totalNegativeAttr);
			negativeComputation.Append('/');
			negativeComputation.Append(sumTblFrequency);
			negativeComputation.Append(')');
		}

		return (positiveComputation.ToString(), negativeComputation.ToString());
	}

	public BayesComputationResult Compute()
	{
		double totalPositiveAttr = _frequencyTbl.Values.Sum(attr => attr.Positive);
		double totalNegativeAttr = _frequencyTbl.Values.Sum(attr => attr.Negative);
		double sumTblFrequency = totalPositiveAttr + totalNegativeAttr;
		int kValue = _frequencyTbl.Count;

		double positivePosterior = _frequencyTbl.Values.Aggregate(1.0, (a, x) =>
		{
			double attrTotalFreq = x.Positive + x.Negative;
			if (_needSmoothing)
				return a * ((x.Positive + Alpha) / (attrTotalFreq + Alpha * kValue));
			return a * (x.Positive / attrTotalFreq);
		}) * (_needSmoothing
			? (totalPositiveAttr + Alpha) / (sumTblFrequency + Alpha * kValue)
			: totalPositiveAttr / sumTblFrequency);

		double negativePosterior = _frequencyTbl.Values.Aggregate(1.0, (a, x) =>
		{
			double attrTotalFreq = x.Positive + x.Negative;
			if (_needSmoothing)
				return a * ((x.Negative + Alpha) / (attrTotalFreq + Alpha * kValue));
			return a * (x.Negative / attrTotalFreq);
		}) * (_needSmoothing
			? (totalNegativeAttr + Alpha) / (sumTblFrequency + Alpha * kValue)
			: totalNegativeAttr / sumTblFrequency);


		// proportional probability
		double posteriorSum = positivePosterior + negativePosterior;
		double proportionalPositiveProb = (positivePosterior / posteriorSum) * 100;
		double proportionalNegativeProb = (negativePosterior / posteriorSum) * 100;

		return new BayesComputationResult(proportionalPositiveProb, proportionalNegativeProb);
	}
}
