using Geco.Core.Database.SqliteModel;

namespace Geco.Core.Database;
public class PromptRepository : DbRepositoryBase
{
	public PromptRepository(string databaseDir) : base(databaseDir)
	{

	}

	// Database table blueprint
	internal override TblSchema[]? TableSchemas =>
	[
		new TblSchema("TblPrompt", [
			new TblField("Category", TblFieldType.Integer),
			new TblField("Content", TblFieldType.Text)
		])
	];

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
			((int)PromptCategory.SearchUserBasedTemp, "Based on the {userTopic}, generate three (3) responses and use a tone like that of a search engine."),
			((int)PromptCategory.SearchCtgBasedTemp, "Using the tone of a search engine and based on the topic of {predefinedTopic}, generate three responses focusing on the {storedPromptRefinement}."),
			((int)PromptCategory.TriggerNotifTemp, "Given the unsustainable action based on {actionTrigger}, the user overstepped the sustainable baseline data of {sustainableBaselineData}. Give a short notification-like message focusing on {storedPromptRefinement}."),
			((int)PromptCategory.LikelihoodWithPrevDataTemp, "The current computed likelihood of sustainable use of mobile is {currentSustainabilityLikelihood}, the computation is as follows {currentLikelihoodComputation}. The values used are based on frequency, specifically: {currentFrequencyData}. The previous week computed likelihood of sustainable use of mobile is {previousSustainabilityLikelihood}, its computation is as follows {previousLikelihoodComputation}. The previous week computation made use of these frequencies: {previousFrequencyData}. Provide an analytical overview regarding the given sustainability data while including recommendations to improve involved sustainable practice. Also, if the previous week data is given, perform a comparison of the current and previous sustainability likelihood computation and value."),
			((int)PromptCategory.LikelihoodNoPrevDataTemp, "The current computed likelihood of sustainable use of mobile is {currentSustainabilityLikelihood}, the computation is as follows {currentLikelihoodComputation}. The values used are based on frequency, specifically: {currentFrequencyData}. Provide an analytical overview regarding the given sustainability data while including recommendation to improve involved sustainable practice."),

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
			((int)PromptCategory.SearchingBrowserRefinement, "Sustainable Browsing Practices"),
		};

		const string insertQuery = "INSERT INTO TblPrompt (Category, Content) VALUES (?, ?)";
		foreach (var (category, content) in prompts)
		{
			await db.ExecuteNonQuery(insertQuery, category, content);
		}
	}

	private async Task<string> FetchRandPromptRefinement(PromptCategory promptCategory)
	{
		await Initialize();

		// Check if categorized as refinement
		if ((int)(promptCategory) < 5)
			throw new Exception("Invalid prompt category");

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		// Randomly select stored prompt refinement
		string? refinement = await db.ExecuteScalar<string>(
				   $"SELECT Content FROM TblPrompt WHERE Category = {(int)promptCategory} ORDER BY RANDOM() LIMIT 1;");
		return refinement ?? throw new Exception($"No prompt refinement found for category {promptCategory}");
	}

	private async Task<string> FetchPromptTemplate(PromptCategory promptCategory)
	{
		await Initialize();

		//Check if categorized as template
		if ((int)(promptCategory) > 4)
			throw new Exception("Invalid prompt category");

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		string? promptTemplate = await db.ExecuteScalar<string>(
				   $"SELECT Content FROM TblPrompt WHERE Category = {(int)promptCategory};");
		return promptTemplate ?? throw new Exception($"No prompt template found for category {promptCategory}");
	}

	private async Task<string> FillPrompt(PromptCategory promptCategory, Dictionary<string, string> promptSpecifics)
	{
		string promptTemplate = await FetchPromptTemplate(promptCategory);

		foreach (var placeholder in promptSpecifics)
		{
			string placeholderKey = "{" + placeholder.Key + "}";
			promptTemplate = promptTemplate.Replace(placeholderKey, placeholder.Value);
		}

		return promptTemplate;
	}

	public static string GetBaselineData(DeviceInteractionTrigger interactionTrigger) => interactionTrigger switch
	{
		DeviceInteractionTrigger.ChargingUnsustainable => "Let your battery naturally deplete to around 20% before charging to about 80%",
		DeviceInteractionTrigger.DeviceUsageUnsustainable => "Spending 7 hours or more daily could potentially damage your eyes",
		DeviceInteractionTrigger.NetworkUsageUnsustainable => "Wi-Fi connection tends to save more energy than using mobile data as the phone is no longer forced to continuously scan for opportunities to connect and maintain steady data flow",
		DeviceInteractionTrigger.LocationUsageUnsustainable => "When location data is enabled, the device uses a combination of GPS, Wi-Fi, and mobile networks to determine and update precise location. Thus, using it requires additional power impacting the device's battery life",
		DeviceInteractionTrigger.BrowserUsageUnsustainable => "Not using the sustainable search feature in this app GECO which utilizes sustainability focused results using Gemini",
		_ => throw new Exception("Unknown Interaction Trigger.")
	};

	public static string GetUnsustainableAction(DeviceInteractionTrigger interactionTrigger) => interactionTrigger switch
	{
		DeviceInteractionTrigger.ChargingUnsustainable => "Mobile charging",
		DeviceInteractionTrigger.DeviceUsageUnsustainable => "Mobile device use or screen time",
		DeviceInteractionTrigger.NetworkUsageUnsustainable => "Connection to mobile data or Wi-Fi",
		DeviceInteractionTrigger.LocationUsageUnsustainable => "Using location services on mobile",
		DeviceInteractionTrigger.BrowserUsageUnsustainable => "Searching in conventional browsers",
		_ => throw new Exception("Unknown Interaction Trigger.")
	};

	public static PromptCategory GetPromptCategory(DeviceInteractionTrigger interactionTrigger) => interactionTrigger switch
	{
		DeviceInteractionTrigger.ChargingUnsustainable => PromptCategory.ChargingRefinement,
		DeviceInteractionTrigger.DeviceUsageUnsustainable => PromptCategory.DeviceUsageRefinement,
		DeviceInteractionTrigger.NetworkUsageUnsustainable => PromptCategory.NetworkUsageRefinement,
		DeviceInteractionTrigger.LocationUsageUnsustainable => PromptCategory.LocationServicesRefinement,
		DeviceInteractionTrigger.BrowserUsageUnsustainable => PromptCategory.SearchingBrowserRefinement,
		_ => throw new Exception("Unknown Interaction Trigger.")
	};

	public static PromptCategory GetPromptCategory(SearchPredefinedTopic predefinedTopic) => predefinedTopic switch
	{
		SearchPredefinedTopic.Energy => PromptCategory.EnergySearchRefinement,
		SearchPredefinedTopic.Waste => PromptCategory.WasteSearchRefinement,
		SearchPredefinedTopic.Fashion => PromptCategory.FashionSearchRefinement,
		SearchPredefinedTopic.Transport => PromptCategory.TransportSearchRefinement,
		_ => throw new Exception("Unknown Interaction Trigger.")
	};

	public async Task<string> BuildSearchUserBasedPrompt(string userTopic) => await FillPrompt(PromptCategory.SearchUserBasedTemp, new Dictionary<string, string>
		{
			{"userTopic", userTopic}
		});

	public async Task<string> BuildSearchCtgBasedPrompt(SearchPredefinedTopic predefinedTopic)
	{
		var promptCategory = GetPromptCategory(predefinedTopic);
		string randomPromptRefinement = await FetchRandPromptRefinement(promptCategory);

		return await FillPrompt(PromptCategory.SearchCtgBasedTemp, new Dictionary<string, string>
			{
				{"predefinedTopic", $"Sustainable {predefinedTopic}"},
				{"storedPromptRefinement", randomPromptRefinement}
			});
	}

	public async Task<string> BuildTriggerNotifPrompt(DeviceInteractionTrigger interactionTrigger)
	{
		var promptCategory = GetPromptCategory(interactionTrigger);
		string actionTrigger = GetUnsustainableAction(interactionTrigger);
		string sustainableBaselineData = GetBaselineData(interactionTrigger);
		string storedPromptRefinement = await FetchRandPromptRefinement(promptCategory);

		return await FillPrompt(PromptCategory.TriggerNotifTemp, new Dictionary<string, string>
			{
				{"actionTrigger", actionTrigger},
				{"sustainableBaselineData", sustainableBaselineData},
				{"storedPromptRefinement", storedPromptRefinement}
			});
	}

	public async Task<string> BuildSustLikelihoodPrompt(string currentSustainabilityLikelihood, string currentLikelihoodComputation,
		string currentFrequencyData) => await FillPrompt(PromptCategory.LikelihoodNoPrevDataTemp, new Dictionary<string, string>
		{
			{"currentSustainabilityLikelihood", currentSustainabilityLikelihood},
			{"currentLikelihoodComputation", currentLikelihoodComputation},
			{"currentFrequencyData", currentFrequencyData}
		});

	public async Task<string> BuildSustLikelihoodPrompt(string currentSustainabilityLikelihood, string currentLikelihoodComputation,
		string currentFrequencyData, string previousSustainabilityLikelihood, string previousLikelihoodComputation, string previousFrequencyData)
		=> await FillPrompt(PromptCategory.LikelihoodWithPrevDataTemp, new Dictionary<string, string>
		{
			{"currentSustainabilityLikelihood", currentSustainabilityLikelihood},
			{"currentLikelihoodComputation", currentLikelihoodComputation},
			{"currentFrequencyData", currentFrequencyData},
			{"previousSustainabilityLikelihood", previousSustainabilityLikelihood},
			{"previousLikelihoodComputation", previousLikelihoodComputation},
			{"previousFrequencyData", previousFrequencyData}
		});
}
