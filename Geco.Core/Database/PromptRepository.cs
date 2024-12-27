using Geco.Core.Database.SqliteModel;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Prompt;

namespace Geco.Core.Database;

public class PromptRepository : DbRepositoryBase
{
	// Database table blueprint
	internal override TblSchema[]? TableSchemas =>
	[
		new TblSchema("TblPrompt", [
			new TblField("Category", TblFieldType.Integer),
			new TblField("Content", TblFieldType.Text)
		])
	];

	public PromptRepository(string databaseDir) : base(databaseDir)
	{
	}

	public async Task<string> GetPrompt(string userTopic) =>
		await BuildPrompt(PromptCategory.SearchUserBasedTemp, new { UserTopic = userTopic });

	public async Task<string> GetPrompt(SearchPredefinedTopic predefinedTopic)
	{
		var promptCategory = GetPromptCategory(predefinedTopic);
		string randomPromptRefinement = await FetchRandPromptRefinement(promptCategory);

		return await BuildPrompt(PromptCategory.SearchCtgBasedTemp,
			new
			{
				PredefinedTopic = $"Sustainable {predefinedTopic}",
				StoredPromptRefinement = randomPromptRefinement
			});
	}

	public async Task<string> GetPrompt(DeviceInteractionTrigger interactionTrigger)
	{
		var promptCategory = GetPromptCategory(interactionTrigger);
		string actionTrigger = GetUnsustainableAction(interactionTrigger);
		string sustainableBaselineData = GetBaselineData(interactionTrigger);
		string storedPromptRefinement = await FetchRandPromptRefinement(promptCategory);

		return await BuildPrompt(PromptCategory.TriggerNotifTemp,
			new
			{
				ActionTrigger = actionTrigger,
				SustainableBaselineData = sustainableBaselineData,
				StoredPromptRefinement = storedPromptRefinement
			});
	}

	public async Task<string> GetLikelihoodPrompt(string currentSustainabilityLikelihood,
		string currentLikelihoodComputation,
		string currentFrequencyData) => await BuildPrompt(PromptCategory.LikelihoodNoPrevDataTemp,
		new
		{
			CurrentSustainabilityLikelihood = currentSustainabilityLikelihood,
			CurrentLikelihoodComputation = currentLikelihoodComputation,
			CurrentFrequencyData = currentFrequencyData
		});

	public async Task<string> GetLikelihoodWithHistoryPrompt(string currentSustainabilityLikelihood,
		string currentLikelihoodComputation,
		string currentFrequencyData, string previousSustainabilityLikelihood, string previousLikelihoodComputation,
		string previousFrequencyData)
		=> await BuildPrompt(PromptCategory.LikelihoodWithPrevDataTemp,
			new
			{
				CurrentSustainabilityLikelihood = currentSustainabilityLikelihood,
				CurrentLikelihoodComputation = currentLikelihoodComputation,
				CurrentFrequencyData = currentFrequencyData,
				PreviousSustainabilityLikelihood = previousSustainabilityLikelihood,
				PreviousLikelihoodComputation = previousLikelihoodComputation,
				PreviousFrequencyData = previousFrequencyData
			});

	protected override async Task InitializeTables()
	{
		await base.InitializeTables();
		await AddInitialPrompts();
	}

	private async Task AddInitialPrompts()
	{
		using var db = await SqliteDb.GetTransient(DatabaseDir);

		var prompts = new List<(int Category, string Content)>
		{
			((int)PromptCategory.SearchUserBasedTemp,
				"Based on the {UserTopic}, generate three (3) responses and use a tone like that of a search engine."),
			((int)PromptCategory.SearchCtgBasedTemp,
				"Using the tone of a search engine and based on the topic of {PredefinedTopic}, generate three responses focusing on the {StoredPromptRefinement}."),
			((int)PromptCategory.TriggerNotifTemp,
				"Given the unsustainable action based on {ActionTrigger}, the user overstepped the sustainable baseline data of {SustainableBaselineData}. Give a notification-like message focusing on {StoredPromptRefinement} and include the given sustainable baseline data in the notification message. The title of the notification should point out the unsustainable action while the description should always be in passive voice and shall be one sentence only."),
			((int)PromptCategory.LikelihoodWithPrevDataTemp,
				"The current computed likelihood of sustainable use of mobile is {CurrentSustainabilityLikelihood}, the computation is as follows {CurrentLikelihoodComputation}. The values used are based on frequency, specifically: {CurrentFrequencyData}. The previous week computed likelihood of sustainable use of mobile is {PreviousSustainabilityLikelihood}, its computation is as follows {PreviousLikelihoodComputation}. The previous week computation made use of these frequencies: {PreviousFrequencyData}. Provide an analytical overview regarding the given sustainability data while including recommendations to improve involved sustainable practice. Also, if the previous week data is given, perform a comparison of the current and previous sustainability likelihood computation and value."),
			((int)PromptCategory.LikelihoodNoPrevDataTemp,
				"The current computed likelihood of sustainable use of mobile is {CurrentSustainabilityLikelihood}, the computation is as follows {CurrentLikelihoodComputation}. The values used are based on frequency, specifically: {CurrentFrequencyData}. Provide an analytical overview regarding the given sustainability data while including recommendation to improve involved sustainable practice."),
			((int)PromptCategory.EnergySearchRefinement, "Awareness and Advocacy"),
			((int)PromptCategory.EnergySearchRefinement, "Comparative Analysis"),
			((int)PromptCategory.EnergySearchRefinement, "Technological Innovations"),
			((int)PromptCategory.EnergySearchRefinement, "Practical Implementation"),
			((int)PromptCategory.EnergySearchRefinement, "Challenges and Solutions"),
			((int)PromptCategory.WasteSearchRefinement, "Community Engagement and Behavioral Change"),
			((int)PromptCategory.WasteSearchRefinement, "Circular Economy Concepts"),
			((int)PromptCategory.WasteSearchRefinement, "Environmental and Economic Impact"),
			((int)PromptCategory.WasteSearchRefinement, "Locally Available Waste Management Initiatives"),
			((int)PromptCategory.WasteSearchRefinement, "Local Policy and Regulations"),
			((int)PromptCategory.FashionSearchRefinement, "Ethical Production and Sourcing"),
			((int)PromptCategory.FashionSearchRefinement, "Affordability and Practicality"),
			((int)PromptCategory.FashionSearchRefinement, "Universal Popularity"),
			((int)PromptCategory.FashionSearchRefinement, "Technological and Material Innovations"),
			((int)PromptCategory.FashionSearchRefinement, "Cultural and Historical Perspectives"),
			((int)PromptCategory.TransportSearchRefinement, "Alternative Fuels"),
			((int)PromptCategory.TransportSearchRefinement, "Urban Mobility"),
			((int)PromptCategory.TransportSearchRefinement, "Innovations in Green Transportation"),
			((int)PromptCategory.TransportSearchRefinement, "Lifestyle Adaptation"),
			((int)PromptCategory.TransportSearchRefinement, "Global Initiatives"),
			((int)PromptCategory.ChargingRefinement, "Mobile Charging Efficiency"),
			((int)PromptCategory.ChargingRefinement, "Renewable Charging Sources"),
			((int)PromptCategory.ChargingRefinement, "Battery Life Preservation"),
			((int)PromptCategory.ChargingRefinement, "Off-Grid Charging Solutions"),
			((int)PromptCategory.ChargingRefinement, "Sustainable Charging Accessories"),
			((int)PromptCategory.DeviceUsageRefinement, "Screen Time Management"),
			((int)PromptCategory.DeviceUsageRefinement, "Digital Well-being Practices"),
			((int)PromptCategory.DeviceUsageRefinement, "Blue Light Exposure Effects"),
			((int)PromptCategory.DeviceUsageRefinement, "Battery-Conscious Screen Time"),
			((int)PromptCategory.DeviceUsageRefinement, "Productivity Tools"),
			((int)PromptCategory.NetworkUsageRefinement, "WiFi vs Mobile Data"),
			((int)PromptCategory.NetworkUsageRefinement, "Optimizing Data Connectivity"),
			((int)PromptCategory.NetworkUsageRefinement, "Energy-Efficient Network Usage"),
			((int)PromptCategory.NetworkUsageRefinement, "Reducing Data Consumption"),
			((int)PromptCategory.NetworkUsageRefinement, "Sustainable Connection Practices"),
			((int)PromptCategory.LocationServicesRefinement, "Efficient Location Tracking"),
			((int)PromptCategory.LocationServicesRefinement, "Reducing Location Services Usage"),
			((int)PromptCategory.LocationServicesRefinement, "Battery-Saving Location Settings"),
			((int)PromptCategory.LocationServicesRefinement, "Optimizing Location Services"),
			((int)PromptCategory.LocationServicesRefinement, "Minimizing Location Data Impact"),
			((int)PromptCategory.SearchingBrowserRefinement, "Benefits of using GECO's sustainable search"),
			((int)PromptCategory.SearchingBrowserRefinement, "Ecological Impact of conventional search engines"),
			((int)PromptCategory.SearchingBrowserRefinement, "GECO's Eco-Friendly Search Options"),
			((int)PromptCategory.SearchingBrowserRefinement, "GECO's Way of Optimizing Search for Sustainability"),
			((int)PromptCategory.SearchingBrowserRefinement, "Sustainable Browsing Practices")
		};

		const string insertQuery = "INSERT INTO TblPrompt (Category, Content) VALUES (?, ?)";
		foreach (var prompt in prompts)
		{
			await db.ExecuteNonQuery(insertQuery, prompt.Category, prompt.Content);
		}
	}

	private async Task<string> FetchRandPromptRefinement(PromptCategory promptCategory)
	{
		await Initialize();

		// Check if categorized as refinement
		if ((int)promptCategory < 5)
			throw new Exception("Invalid prompt category");

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		// Randomly select stored prompt refinement
		string? refinement = await db.ExecuteScalar<string>(
			$"SELECT Content FROM TblPrompt WHERE Category = {(int)promptCategory} ORDER BY RANDOM() LIMIT 1");
		return refinement ?? throw new Exception($"No prompt refinement found for category {promptCategory}");
	}

	private async Task<string> FetchPromptTemplate(PromptCategory promptCategory)
	{
		await Initialize();

		//Check if categorized as template
		if ((int)promptCategory > 4)
			throw new Exception("Invalid prompt category");

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		string? promptTemplate = await db.ExecuteScalar<string>(
			$"SELECT Content FROM TblPrompt WHERE Category = {(int)promptCategory}");
		return promptTemplate ?? throw new Exception($"No prompt template found for category {promptCategory}");
	}

	private async Task<string> BuildPrompt(PromptCategory promptCategory, object promptSpecifics)
	{
		string promptTemplate = await FetchPromptTemplate(promptCategory);
		string promptResult = StringHelpers.FormatString(promptTemplate, promptSpecifics);

		return promptResult;
	}

	private static string GetBaselineData(DeviceInteractionTrigger interactionTrigger) => interactionTrigger switch
	{
		DeviceInteractionTrigger.ChargingUnsustainable =>
			"Let your battery naturally deplete to around 20% before charging to about 80%",
		DeviceInteractionTrigger.DeviceUsageUnsustainable =>
			"Spending 7 hours or more daily could potentially damage your eyes",
		DeviceInteractionTrigger.NetworkUsageUnsustainable =>
			"Wi-Fi connection tends to save more energy than using mobile data as the phone is no longer forced to continuously scan for opportunities to connect and maintain steady data flow",
		DeviceInteractionTrigger.LocationUsageUnsustainable =>
			"When location data is enabled, the device uses a combination of GPS, Wi-Fi, and mobile networks to determine and update precise location. Thus, using it requires additional power impacting the device's battery life",
		DeviceInteractionTrigger.BrowserUsageUnsustainable =>
			"Not using the sustainable search feature in this app GECO which utilizes sustainability focused results using Gemini",
		_ => throw new Exception("Unknown Interaction Trigger.")
	};

	private static string GetUnsustainableAction(DeviceInteractionTrigger interactionTrigger) =>
		interactionTrigger switch
		{
			DeviceInteractionTrigger.ChargingUnsustainable => "Mobile charging",
			DeviceInteractionTrigger.DeviceUsageUnsustainable => "Mobile device use or screen time",
			DeviceInteractionTrigger.NetworkUsageUnsustainable => "Connection to mobile data or Wi-Fi",
			DeviceInteractionTrigger.LocationUsageUnsustainable => "Using location services on mobile",
			DeviceInteractionTrigger.BrowserUsageUnsustainable => "Searching in conventional browsers",
			_ => throw new Exception("Unknown Interaction Trigger.")
		};

	private static PromptCategory GetPromptCategory(DeviceInteractionTrigger interactionTrigger) =>
		interactionTrigger switch
		{
			DeviceInteractionTrigger.ChargingUnsustainable => PromptCategory.ChargingRefinement,
			DeviceInteractionTrigger.DeviceUsageUnsustainable => PromptCategory.DeviceUsageRefinement,
			DeviceInteractionTrigger.NetworkUsageUnsustainable => PromptCategory.NetworkUsageRefinement,
			DeviceInteractionTrigger.LocationUsageUnsustainable => PromptCategory.LocationServicesRefinement,
			DeviceInteractionTrigger.BrowserUsageUnsustainable => PromptCategory.SearchingBrowserRefinement,
			_ => throw new Exception("Unknown Interaction Trigger.")
		};

	private static PromptCategory GetPromptCategory(SearchPredefinedTopic predefinedTopic) => predefinedTopic switch
	{
		SearchPredefinedTopic.Energy => PromptCategory.EnergySearchRefinement,
		SearchPredefinedTopic.Waste => PromptCategory.WasteSearchRefinement,
		SearchPredefinedTopic.Fashion => PromptCategory.FashionSearchRefinement,
		SearchPredefinedTopic.Transport => PromptCategory.TransportSearchRefinement,
		_ => throw new Exception("Unknown Search Predefined Topic.")
	};
}
