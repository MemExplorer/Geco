using System.Diagnostics;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.Components;
using Xunit.Abstractions;
using SchemaType = GoogleGeminiSDK.Models.Components.SchemaType;

namespace Geco.Core.Test;

public class PromptBuilderTest
{
	const string GEMINI_API_KEY = "Your API Key";
	private readonly ITestOutputHelper _output;

	GeminiSettings NotificationConfig { get; }
	GeminiSettings WeeklyReportConfig { get; }
	GeminiChat ChatClient { get; }
	PromptRepository PromptRepo { get; }
	public PromptBuilderTest(ITestOutputHelper output)
	{
		_output = output;
		string dbPath = Environment.CurrentDirectory;
		PromptRepo = new PromptRepository(dbPath);
		NotificationConfig = new GeminiSettings
		{
			SystemInstructions =
				"You are Geco, a large language model based on Google Gemini. You are developed by SS Bois. Your response should always be sustainability focused. The contents of the 'FullContent' property must be presented in **Markdown**.",
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
				SchemaType.ARRAY,
				Items: new Schema(SchemaType.OBJECT,
					Properties: new Dictionary<string, Schema>
					{
						{ "NotificationTitle", new Schema(SchemaType.STRING) },
						{ "NotificationDescription", new Schema(SchemaType.STRING) },
						{ "FullContent", new Schema(SchemaType.STRING) }
					},
					Required: ["NotificationTitle", "NotificationDescription", "FullContent"]
				)
			)
		};
		WeeklyReportConfig = new GeminiSettings
		{
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
				SchemaType.ARRAY,
				Items: new Schema(SchemaType.OBJECT,
					Properties: new Dictionary<string, Schema>
					{
						{ "NotificationTitle", new Schema(SchemaType.STRING) },
						{ "NotificationDescription", new Schema(SchemaType.STRING) },
						{ "Overview", new Schema(SchemaType.STRING) },
						{ "ReportBreakdown", new Schema(SchemaType.STRING) },
						{ "ComputeBreakdown", new Schema(SchemaType.STRING) }
					},
					Required:
					[
						"NotificationTitle", "NotificationDescription", "Overview", "ReportBreakdown",
						"ComputeBreakdown"
					]
				)
			)
		};
		ChatClient = new GeminiChat(GEMINI_API_KEY, "gemini-1.5-flash-latest");
	}

	string GetSustainabilityLevel(double probability) => probability switch
	{
		>= 90 => "High Sustainability",
		>= 75 and < 90 => "Sustainable",
		>= 60 and < 75 => "Close to Sustainable",
		>= 45 and < 60 => "Average Sustainability",
		>= 30 and < 45 => "Signs of Unsustainability",
		>= 15 and < 30 => "Unsustainable",
		_ => "Crisis level",
	};

	[Fact]
	async Task TriggerNotificationPromptTest()
	{
		var triggerNotifPrompt = await PromptRepo.GetPrompt(DeviceInteractionTrigger.ChargingUnsustainable);

		// ensure all placeholders are replaced with the appropriate values
		Debug.Assert(!new string[] {
			"ActionTrigger",
			"SustainableBaselineData",
			"StoredPromptRefinement"
		}
		.All(x => triggerNotifPrompt.Contains($"{{{x}}}")));

		var triggerNotifResult = await ChatClient.SendMessage(triggerNotifPrompt, settings: NotificationConfig);
		_output.WriteLine($"Constructed Prompt:\n {triggerNotifPrompt}");
		_output.WriteLine($"Generated Output:\n {triggerNotifResult}");
	}

	[Fact]
	async Task WeeklyReportWithoutPrevousDataTest()
	{
		var currWeekBayesInst = new BayesTheorem();
		currWeekBayesInst.AppendData("Charging", 2, 7);
		currWeekBayesInst.AppendData("Usage", 8, 12);
		currWeekBayesInst.AppendData("Network", 6, 10);

		// gets values we need for prompt
		var currWeekComputationResult = currWeekBayesInst.Compute();
		var currWeekComputationStr = currWeekBayesInst.GetComputationSolution();
		double currSustainableProportionalProbability = Math.Round(currWeekComputationResult.PositiveProbability, 2);
		string currWeekSusLvl = GetSustainabilityLevel(currSustainableProportionalProbability);
		string weeklyReportWithoutPrevPrompt = await PromptRepo.GetLikelihoodPrompt(currSustainableProportionalProbability, currWeekComputationStr.PositiveComputation, currWeekSusLvl);

		// ensure all placeholders are replaced with the appropriate values
		Debug.Assert(!new string[] {
			"CurrentSustainabilityLikelihood",
			"CurrentLikelihoodComputation",
			"SustainabilityLevel"
		}
		.All(x => weeklyReportWithoutPrevPrompt.Contains($"{{{x}}}")));

		var weeklyReportWithoutPrevResult = await ChatClient.SendMessage(weeklyReportWithoutPrevPrompt, settings: WeeklyReportConfig);
		_output.WriteLine($"Constructed Prompt:\n {weeklyReportWithoutPrevPrompt}");
		_output.WriteLine($"Generated Output:\n {weeklyReportWithoutPrevResult}");
	}

	[Fact]
	async Task WeeklyReportWithPreviousDataTest()
	{
		var currWeekBayesInst = new BayesTheorem();
		currWeekBayesInst.AppendData("Charging", 2, 7);
		currWeekBayesInst.AppendData("Usage", 8, 12);
		currWeekBayesInst.AppendData("Network", 6, 10);

		// gets values we need for prompt
		var currWeekComputationResult = currWeekBayesInst.Compute();
		var currWeekComputationStr = currWeekBayesInst.GetComputationSolution();
		double currSustainableProportionalProbability = Math.Round(currWeekComputationResult.PositiveProbability, 2);

		var prevWeekBayesInst = new BayesTheorem();
		prevWeekBayesInst.AppendData("Charging", 2, 0);
		prevWeekBayesInst.AppendData("Usage", 8, 0);
		prevWeekBayesInst.AppendData("Network", 6, 0);

		// gets values we need for prompt
		var prevWeekComputationResult = prevWeekBayesInst.Compute();
		var prevWeekComputationStr = prevWeekBayesInst.GetComputationSolution();
		double prevSustainableProportionalProbability = Math.Round(prevWeekComputationResult.PositiveProbability, 2);
		string prevWeekSusLvl = GetSustainabilityLevel(prevSustainableProportionalProbability);

		// prompts
		string weeklyReportWithPrevPrompt = await PromptRepo.GetLikelihoodWithHistoryPrompt(currSustainableProportionalProbability,
			currWeekComputationStr.PositiveComputation, prevSustainableProportionalProbability,
			prevWeekComputationStr.PositiveComputation, prevWeekSusLvl);

		// ensure all placeholders are replaced with the appropriate values
		Debug.Assert(!new string[] {
			"CurrentSustainabilityLikelihood",
			"CurrentLikelihoodComputation",
			"PreviousSustainabilityLikelihood",
			"PreviousLikelihoodComputation",
			"SustainabilityLevel"
		}
		.All(x => weeklyReportWithPrevPrompt.Contains($"{{{x}}}")));

		var weeklyReportWithPrevResult = await ChatClient.SendMessage(weeklyReportWithPrevPrompt, settings: WeeklyReportConfig);
		_output.WriteLine($"Constructed Prompt:\n {weeklyReportWithPrevPrompt}");
		_output.WriteLine($"Generated Output:\n {weeklyReportWithPrevResult}");
	}
}
