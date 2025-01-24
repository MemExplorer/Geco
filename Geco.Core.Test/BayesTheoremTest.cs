using System.Diagnostics;
using Geco.Core.Database;
using Xunit.Abstractions;

namespace Geco.Core.Test;

public class BayesTheoremTest
{
	private readonly ITestOutputHelper _output;

	public BayesTheoremTest(ITestOutputHelper output) =>
		_output = output;

	[Fact]
	void StabilityTest()
	{
		var bayesInst = new BayesTheorem();
		bayesInst.AppendData("Charging", 2, 7);
		bayesInst.AppendData("Usage", 8, 12);
		bayesInst.AppendData("Network", 6, 10);
		var computation = bayesInst.Compute();
		var computationStr = bayesInst.GetComputationInString();
		_output.WriteLine("Positive: " + computationStr.PositiveComputation);
		_output.WriteLine("Negative: " + computationStr.NegativeComputation);
		string? frequencyStr = bayesInst.GetFrequencyInString();
		_output.WriteLine("Frequency: \n" + frequencyStr);
		Debug.Assert(computation.IsPositive == false);
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
		var currWeekComputationStr = currWeekBayesInst.GetComputationInString();
		string currWeekFrequencyStr = currWeekBayesInst.GetFrequencyInString();
		double currSustainableProportionalProbability = Math.Round(currWeekComputationResult.PositiveProbs, 2);
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
		var currWeekComputationStr = currWeekBayesInst.GetComputationInString();
		string currWeekFrequencyStr = currWeekBayesInst.GetFrequencyInString();
		double currSustainableProportionalProbability = Math.Round(currWeekComputationResult.PositiveProbs, 2);

		var prevWeekBayesInst = new BayesTheorem();
		prevWeekBayesInst.AppendData("Charging", 2, 0);
		prevWeekBayesInst.AppendData("Usage", 8, 0);
		prevWeekBayesInst.AppendData("Network", 6, 0);
		// gets values we need for prompt
		var prevWeekComputationResult = prevWeekBayesInst.Compute();
		var prevWeekComputationStr = prevWeekBayesInst.GetComputationInString();
		string prevWeekFrequencyStr = prevWeekBayesInst.GetFrequencyInString();
		double prevSustainableProportionalProbability = Math.Round(prevWeekComputationResult.PositiveProbs, 2);
		string? finalPrompt = await promptRepo.GetLikelihoodWithHistoryPrompt(currSustainableProportionalProbability,
			currWeekComputationStr.PositiveComputation, currWeekFrequencyStr, prevSustainableProportionalProbability,
			prevWeekComputationStr.PositiveComputation, prevWeekFrequencyStr);
		_output.WriteLine(finalPrompt);
	}
}
