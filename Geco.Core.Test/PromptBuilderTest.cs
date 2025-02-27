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
	private GeminiSettings NotificationConfig { get; }
	private GeminiSettings WeeklyReportConfig { get; }
	private GeminiSettings SearchConfig { get; }
	private GeminiChat ChatClient { get; }
	private PromptRepository PromptRepo { get; }

	private static readonly string[] WeeklyReportWithPrevPlaceholders =
	[
		"CurrentSustainabilityLikelihood",
		"CurrentLikelihoodComputation",
		"PreviousSustainabilityLikelihood",
		"PreviousLikelihoodComputation",
		"SustainabilityLevel"
	];

	private static readonly string[] WeeklyReportWithoutPrevPlaceholders =
	[
		"CurrentSustainabilityLikelihood",
		"CurrentLikelihoodComputation",
		"SustainabilityLevel"
	];

	private static readonly string[] NotificationPromptPlaceholders =
	[
		"ActionTrigger",
		"SustainableBaselineData",
		"StoredPromptRefinement"
	];

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
		SearchConfig = new GeminiSettings
		{
			Conversational = false,
			SystemInstructions = """
			                     You are Geco, a large language model based on Google Gemini. 
			                     You are developed by SS Bois. 
			                     You are also a search engine that gives an AI overview. 
			                     Do not include overview or ai overview in the content.
			                     The information from your AI Overview is based on what you know and also from the 'Search Result' that is in json format
			                     Your response should always be sustainability focused.
			                     All responses must be presented in **Markdown**.
			                     """
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
		_ => "Crisis level"
	};

	[Fact]
	async Task SearchAiOverviewPromptTest()
	{
		const string searchTopic = "Food";

		// generated using chatgpt
		const string searchResultSampleData = """
		                                      [ 
		                                          {
		                                              "Title": "Creating a Sustainable Food Future | World Resources Report",
		                                              "Url": "https://research.wri.org/wrr-food",
		                                              "Description": "The report offers a five-course menu of solutions to ensure we can feed 10 billion people by 2050 without increasing emissions, fueling deforestation or exacerbating poverty.",
		                                              "PageAge": "0001-01-01T00:00:00",
		                                              "Profile": {
		                                                  "Name": "Wri",
		                                                  "Url": "https://research.wri.org/wrr-food",
		                                                  "LongName": "research.wri.org",
		                                                  "Img": "https://imgs.search.brave.com/hX4LAPiBVS7CLx8R_5Zpi8HTZ_aaBrIoGDrGwP3tamI/rs:fit:32:32:1:0/g:ce/aHR0cDovL2Zhdmlj/b25zLnNlYXJjaC5i/cmF2ZS5jb20vaWNv/bnMvNThjY2NhZjM4/Yzg5MDNiZDdmMDdl/ZjFmODUyNWE2Nzdk/YWNiMjFlNGI5YWIz/NjMwM2E1MDYzMDE1/NzFmM2Y2MC9yZXNl/YXJjaC53cmkub3Jn/Lw"
		                                              },
		                                              "MetaUrl": {
		                                                  "Scheme": "https",
		                                                  "Netloc": "research.wri.org",
		                                                  "Hostname": "research.wri.org",
		                                                  "Favicon": "https://imgs.search.brave.com/hX4LAPiBVS7CLx8R_5Zpi8HTZ_aaBrIoGDrGwP3tamI/rs:fit:32:32:1:0/g:ce/aHR0cDovL2Zhdmlj/b25zLnNlYXJjaC5i/cmF2ZS5jb20vaWNv/bnMvNThjY2NhZjM4/Yzg5MDNiZDdmMDdl/ZjFmODUyNWE2Nzdk/YWNiMjFlNGI5YWIz/NjMwM2E1MDYzMDE1/NzFmM2Y2MC9yZXNl/YXJjaC53cmkub3Jn/Lw",
		                                                  "Path": "\u203A wrr-food"
		                                              },
		                                              "ExtraSnippets": [
		                                                  "The ‘World Resources Report: Creating a Sustainable Food Future’ shows that it is possible – but there is no silver bullet.",
		                                                  "The menu items for a sustainable food future, described and analyzed in our five courses, focus heavily on technical opportunities.",
		                                                  "The report offers a five-course menu of solutions to ensure we can feed 10 billion people by 2050 without increasing emissions, fueling deforestation or exacerbating poverty.",
		                                                  "Tree cover gain may indicate a number of potential activities, including natural forest growth or the crop rotation cycle of tree plantations."
		                                              ]
		                                          },
		                                          {
		                                              "Title": "The Science Behind a Healthy Diet | Harvard T.H. Chan",
		                                              "Url": "https://www.hsph.harvard.edu/nutritionsource/",
		                                              "Description": "Harvard experts share research-backed insights on nutrition, dietary patterns, and their impact on long-term health.",
		                                              "PageAge": "0001-01-01T00:00:00",
		                                              "Profile": {
		                                                  "Name": "Harvard T.H. Chan",
		                                                  "Url": "https://www.hsph.harvard.edu/",
		                                                  "LongName": "Harvard School of Public Health",
		                                                  "Img": "https://www.hsph.harvard.edu/wp-content/uploads/sites/30/2019/04/favicon.ico"
		                                              },
		                                              "MetaUrl": {
		                                                  "Scheme": "https",
		                                                  "Netloc": "www.hsph.harvard.edu",
		                                                  "Hostname": "hsph.harvard.edu",
		                                                  "Favicon": "https://www.hsph.harvard.edu/wp-content/uploads/sites/30/2019/04/favicon.ico",
		                                                  "Path": "/nutritionsource/"
		                                              },
		                                              "ExtraSnippets": [
		                                                  "Eating a balanced diet rich in whole foods can improve overall health and reduce disease risk.",
		                                                  "Research highlights the benefits of plant-based diets for longevity and well-being.",
		                                                  "Harvard’s Healthy Eating Plate provides a visual guide to proper nutrition."
		                                              ]
		                                          },
		                                          {
		                                              "Title": "Food Waste: A Global Challenge | FAO",
		                                              "Url": "https://www.fao.org/food-loss-and-food-waste/en/",
		                                              "Description": "The FAO provides data and solutions to combat food loss and waste, a major issue affecting global food security.",
		                                              "PageAge": "0001-01-01T00:00:00",
		                                              "Profile": {
		                                                  "Name": "FAO",
		                                                  "Url": "https://www.fao.org/",
		                                                  "LongName": "Food and Agriculture Organization of the United Nations",
		                                                  "Img": "https://www.fao.org/favicon.ico"
		                                              },
		                                              "MetaUrl": {
		                                                  "Scheme": "https",
		                                                  "Netloc": "www.fao.org",
		                                                  "Hostname": "fao.org",
		                                                  "Favicon": "https://www.fao.org/favicon.ico",
		                                                  "Path": "/food-loss-and-food-waste/en/"
		                                              },
		                                              "ExtraSnippets": [
		                                                  "Roughly one-third of the food produced globally is lost or wasted.",
		                                                  "Reducing food waste is crucial for sustainability and addressing world hunger.",
		                                                  "FAO proposes innovative solutions to minimize food waste in supply chains."
		                                              ]
		                                          },
		                                          {
		                                              "Title": "The Future of Alternative Proteins | Good Food Institute",
		                                              "Url": "https://gfi.org/science/",
		                                              "Description": "Exploring the science and market potential of plant-based, cultivated, and fermentation-derived proteins as sustainable food sources.",
		                                              "PageAge": "0001-01-01T00:00:00",
		                                              "Profile": {
		                                                  "Name": "GFI",
		                                                  "Url": "https://gfi.org/",
		                                                  "LongName": "Good Food Institute",
		                                                  "Img": "https://gfi.org/wp-content/uploads/2020/07/favicon.ico"
		                                              },
		                                              "MetaUrl": {
		                                                  "Scheme": "https",
		                                                  "Netloc": "gfi.org",
		                                                  "Hostname": "gfi.org",
		                                                  "Favicon": "https://gfi.org/wp-content/uploads/2020/07/favicon.ico",
		                                                  "Path": "/science/"
		                                              },
		                                              "ExtraSnippets": [
		                                                  "Alternative proteins could help reduce the environmental impact of food production.",
		                                                  "Cellular agriculture and fermentation are revolutionizing the food industry.",
		                                                  "Plant-based meat alternatives are gaining popularity for health and sustainability reasons."
		                                              ]
		                                          },
		                                          {
		                                              "Title": "The Mediterranean Diet: Benefits and Meal Plan | Healthline",
		                                              "Url": "https://www.healthline.com/nutrition/mediterranean-diet-meal-plan",
		                                              "Description": "A complete guide to the Mediterranean diet, including its health benefits and sample meal plans.",
		                                              "PageAge": "0001-01-01T00:00:00",
		                                              "Profile": {
		                                                  "Name": "Healthline",
		                                                  "Url": "https://www.healthline.com/",
		                                                  "LongName": "Healthline Media",
		                                                  "Img": "https://www.healthline.com/favicon.ico"
		                                              },
		                                              "MetaUrl": {
		                                                  "Scheme": "https",
		                                                  "Netloc": "www.healthline.com",
		                                                  "Hostname": "healthline.com",
		                                                  "Favicon": "https://www.healthline.com/favicon.ico",
		                                                  "Path": "/nutrition/mediterranean-diet-meal-plan"
		                                              },
		                                              "ExtraSnippets": [
		                                                  "The Mediterranean diet emphasizes whole foods, healthy fats, and lean proteins.",
		                                                  "Studies show it reduces the risk of heart disease and promotes longevity.",
		                                                  "A Mediterranean meal plan includes olive oil, fish, nuts, fruits, and vegetables."
		                                              ]
		                                          },
		                                          {
		                                              "Title": "Understanding the Farm-to-Table Movement | National Geographic",
		                                              "Url": "https://www.nationalgeographic.com/food/features/farm-to-table/",
		                                              "Description": "An in-depth look at the farm-to-table movement and its impact on local food systems and sustainability.",
		                                              "PageAge": "0001-01-01T00:00:00",
		                                              "Profile": {
		                                                  "Name": "National Geographic",
		                                                  "Url": "https://www.nationalgeographic.com/",
		                                                  "LongName": "National Geographic",
		                                                  "Img": "https://www.nationalgeographic.com/favicon.ico"
		                                              },
		                                              "MetaUrl": {
		                                                  "Scheme": "https",
		                                                  "Netloc": "www.nationalgeographic.com",
		                                                  "Hostname": "nationalgeographic.com",
		                                                  "Favicon": "https://www.nationalgeographic.com/favicon.ico",
		                                                  "Path": "/food/features/farm-to-table/"
		                                              },
		                                              "ExtraSnippets": [
		                                                  "Farm-to-table emphasizes fresh, local ingredients and direct sourcing.",
		                                                  "Supporting local farmers helps strengthen regional economies.",
		                                                  "Sustainable food practices benefit both consumers and the environment."
		                                              ]
		                                          }
		                                      ]
		                                      """;

		const string searchPrompt = $"Topic: {searchTopic}\nSearch Result: {searchResultSampleData}";
		var searchAiOverview = await ChatClient.SendMessage(searchPrompt, settings: SearchConfig);
		_output.WriteLine($"Constructed Prompt:\n {searchPrompt}");
		_output.WriteLine($"Generated Output:\n {searchAiOverview}");
	}

	[Fact]
	async Task TriggerNotificationPromptTest()
	{
		string triggerNotifPrompt = await PromptRepo.GetPrompt(DeviceInteractionTrigger.ChargingUnsustainable);

		// ensure all placeholders are replaced with the appropriate values
		Debug.Assert(!NotificationPromptPlaceholders.All(x => triggerNotifPrompt.Contains($"{{{x}}}")));

		var triggerNotifResult = await ChatClient.SendMessage(triggerNotifPrompt, settings: NotificationConfig);
		_output.WriteLine($"Constructed Prompt:\n {triggerNotifPrompt}");
		_output.WriteLine($"Generated Output:\n {triggerNotifResult}");
	}

	[Fact]
	async Task WeeklyReportWithoutPreviousDataTest()
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
		string weeklyReportWithoutPrevPrompt = await PromptRepo.GetLikelihoodPrompt(
			currSustainableProportionalProbability, currWeekComputationStr.PositiveComputation, currWeekSusLvl);

		// ensure all placeholders are replaced with the appropriate values
		Debug.Assert(!WeeklyReportWithoutPrevPlaceholders.All(x => weeklyReportWithoutPrevPrompt.Contains($"{{{x}}}")));

		var weeklyReportWithoutPrevResult =
			await ChatClient.SendMessage(weeklyReportWithoutPrevPrompt, settings: WeeklyReportConfig);
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
		string weeklyReportWithPrevPrompt = await PromptRepo.GetLikelihoodWithHistoryPrompt(
			currSustainableProportionalProbability,
			currWeekComputationStr.PositiveComputation, prevSustainableProportionalProbability,
			prevWeekComputationStr.PositiveComputation, prevWeekSusLvl);

		// ensure all placeholders are replaced with the appropriate values
		Debug.Assert(!WeeklyReportWithPrevPlaceholders.All(x => weeklyReportWithPrevPrompt.Contains($"{{{x}}}")));

		var weeklyReportWithPrevResult =
			await ChatClient.SendMessage(weeklyReportWithPrevPrompt, settings: WeeklyReportConfig);
		_output.WriteLine($"Constructed Prompt:\n {weeklyReportWithPrevPrompt}");
		_output.WriteLine($"Generated Output:\n {weeklyReportWithPrevResult}");
	}
}
