using System.Diagnostics;
using System.Text;
using Geco.Core.Database;
using Xunit.Abstractions;

namespace Geco.Core.Test;

public class BayesTheoremTest
{
	private readonly ITestOutputHelper _output;

	public BayesTheoremTest(ITestOutputHelper output) =>
		_output = output;

	string GetFrequencyInString(IDictionary<string, BayesTheoremAttribute> tbl)
	{
		var sb = new StringBuilder();
		sb.AppendLine("Attribute Name | Positive | Negative ");
		foreach (var k in tbl)
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

	[Fact]
	void StabilityTest()
	{
		var bayesInst = new BayesTheorem();
		bayesInst.AppendData("Charging", 2, 7);
		bayesInst.AppendData("Usage", 8, 12);
		bayesInst.AppendData("Network", 6, 10);
		var computation = bayesInst.Compute();
		var computationStr = bayesInst.GetComputationSolution();
		_output.WriteLine("Positive: " + computationStr.PositiveComputation);
		_output.WriteLine("Negative: " + computationStr.NegativeComputation);
		string? frequencyStr = GetFrequencyInString(bayesInst.GetFrequencyData());
		_output.WriteLine("Frequency: \n" + frequencyStr);
		Debug.Assert((computation.PositiveProbability > computation.NegativeProbability) == false);
	}

	[Fact]
	async Task BayesWithPromptTest()
	{
		string dbPath = Environment.CurrentDirectory;
		var promptRepo = new PromptRepository(dbPath);
		var currWeekBayesInst = new BayesTheorem();
		currWeekBayesInst.AppendData("Charging", 2, 7);
		currWeekBayesInst.AppendData("Usage", 8, 12);
		currWeekBayesInst.AppendData("Network", 6, 10);
		// gets values we need for prompt
		var currWeekComputationResult = currWeekBayesInst.Compute();
		var currWeekComputationStr = currWeekBayesInst.GetComputationSolution();
		string currWeekFrequencyStr = GetFrequencyInString(currWeekBayesInst.GetFrequencyData());
		double currSustainableProportionalProbability = Math.Round(currWeekComputationResult.PositiveProbability, 2);
		string? finalPrompt = await promptRepo.GetLikelihoodPrompt(currSustainableProportionalProbability,
			currWeekComputationStr.PositiveComputation, currWeekFrequencyStr);
		_output.WriteLine(finalPrompt);
	}

	[Fact]
	async Task BayesWithPreviousWeekPromptTest()
	{
		string dbPath = Environment.CurrentDirectory;
		var promptRepo = new PromptRepository(dbPath);
		var currWeekBayesInst = new BayesTheorem();
		currWeekBayesInst.AppendData("Charging", 2, 7);
		currWeekBayesInst.AppendData("Usage", 8, 12);
		currWeekBayesInst.AppendData("Network", 6, 10);
		// gets values we need for prompt
		var currWeekComputationResult = currWeekBayesInst.Compute();
		var currWeekComputationStr = currWeekBayesInst.GetComputationSolution();
		string currWeekFrequencyStr = GetFrequencyInString(currWeekBayesInst.GetFrequencyData());
		double currSustainableProportionalProbability = Math.Round(currWeekComputationResult.PositiveProbability, 2);

		var prevWeekBayesInst = new BayesTheorem();
		prevWeekBayesInst.AppendData("Charging", 2, 0);
		prevWeekBayesInst.AppendData("Usage", 8, 0);
		prevWeekBayesInst.AppendData("Network", 6, 0);
		// gets values we need for prompt
		var prevWeekComputationResult = prevWeekBayesInst.Compute();
		var prevWeekComputationStr = prevWeekBayesInst.GetComputationSolution();
		string prevWeekFrequencyStr = GetFrequencyInString(prevWeekBayesInst.GetFrequencyData());
		double prevSustainableProportionalProbability = Math.Round(prevWeekComputationResult.PositiveProbability, 2);
		string? finalPrompt = await promptRepo.GetLikelihoodWithHistoryPrompt(currSustainableProportionalProbability,
			currWeekComputationStr.PositiveComputation, prevSustainableProportionalProbability,
			prevWeekComputationStr.PositiveComputation, "Status");
		_output.WriteLine(finalPrompt);
	}
}
