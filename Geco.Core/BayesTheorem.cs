using System.Text;

namespace Geco.Core;

public record BayesTheoremAttribute(int Positive, int Negative);

public class BayesTheorem
{
	readonly Dictionary<string, BayesTheoremAttribute> _frequencyTbl = new();

	public void AppendData(string attrName, int positive, int negative) =>
		_frequencyTbl.Add(attrName, new BayesTheoremAttribute(positive, negative));

	public string GetFrequencyInString()
	{
		var sb = new StringBuilder();
		sb.AppendLine("Attribute Name | Positive | Negative ");
		foreach (var k in _frequencyTbl)
		{
			sb.Append(k.Key);
			sb.Append(" | ");
			sb.Append(k.Value.Positive);
			sb.Append(" | ");
			sb.Append(k.Value.Negative);
			sb.AppendLine();
		}
		
		return sb.ToString();
	}
	
	public (string PositiveComputation, string NegativeComputation) GetComputationInString()
	{
		double totalPositiveAttr = _frequencyTbl.Values.Sum(attr => attr.Positive);
		double totalNegativeAttr = _frequencyTbl.Values.Sum(attr => attr.Negative);
		double sumTblFrequency = totalPositiveAttr + totalNegativeAttr;

		var positiveComputation = new StringBuilder();
		var negativeComputation = new StringBuilder();
		foreach (var x in _frequencyTbl.Values)
		{
			double attrTotalFreq = x.Positive + x.Negative;
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
		
		return (positiveComputation.ToString(), negativeComputation.ToString());
	}
	
	public (bool IsPositive, double PositiveProbs, double NegativeProbs) Compute()
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

		return (positivePosterior > negativePosterior, proportionalPositiveProb, proportionalNegativeProb);
	}
}
