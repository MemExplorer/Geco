using System.Globalization;
using Geco.Core.Database.SqliteModel;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Prompt;

namespace Geco.Core.Database;

public class PromptRepository : DbRepositoryBase
{
	// Database table blueprint
	internal override TblSchema[] TableSchemas =>
	[
		new("TblPrompt", [
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

	public async Task<string> GetLikelihoodPrompt(double currentSustainabilityLikelihood,
		string currentLikelihoodComputation, string sustainabilityLevel) => await BuildPrompt(PromptCategory.LikelihoodNoPrevDataTemp,
		new
		{
			CurrentSustainabilityLikelihood =
				currentSustainabilityLikelihood.ToString(CultureInfo.InvariantCulture) + "%",
			CurrentLikelihoodComputation = currentLikelihoodComputation,
			SustainabilityLevel = sustainabilityLevel
		});

	public async Task<string> GetLikelihoodWithHistoryPrompt(double currentSustainabilityLikelihood,
		string currentLikelihoodComputation, double previousSustainabilityLikelihood, string previousLikelihoodComputation, string sustainabilityLevel) =>
		await BuildPrompt(PromptCategory.LikelihoodWithPrevDataTemp,
			new
			{
				CurrentSustainabilityLikelihood =
					currentSustainabilityLikelihood.ToString(CultureInfo.InvariantCulture) + "%",
				CurrentLikelihoodComputation = currentLikelihoodComputation,
				PreviousSustainabilityLikelihood =
					previousSustainabilityLikelihood.ToString(CultureInfo.InvariantCulture) + "%",
				PreviousLikelihoodComputation = previousLikelihoodComputation,
				SustainabilityLevel = sustainabilityLevel
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
				"""
				**Purpose:** Create a notification and an analytical overview based on detected unsustainable actions, using provided placeholders for contextual adaptability.  

				## **Notification Requirements**  

				1. **Notification Title (NotificationTitle):**  
				   - Clearly indicate the unsustainable action.  
				   - Use language that mildly highlights the action as non-ideal without implying habitual behavior.  

				2. **Notification Description (NotificationDescription):**  
				   - Must be in **passive voice**.  
				   - Limited to **one sentence only**.  
				   - Include the **exact and complete sustainable baseline data** provided in `{SustainableBaselineData}`.  

				## **Full Analytical Overview Requirements**  

				**Format:** Create the content in **HTML**.  

				**Tone:** Mild and suggestive, avoiding accusatory language.  

				**Content Structure (But not limited to):**  
				1. **Introduction:** Briefly introduce the context of the detected unsustainable action.  
				2. **Importance of Awareness:** Explain why the user should be aware of this action.  
				3. **Benefits of Correction:**  
				   - Highlight how correcting the action aligns with sustainability goals.  
				   - Provide specific benefits, if applicable, of maintaining the sustainable baseline data.  

				## Placeholders to Use: 
				- **{ActionTrigger}:** Represents the specific action detected.  
				- **{SustainableBaselineData}:** The exact sustainability data that was exceeded or not maintained.  
				- **{StoredPromptRefinement}:** Focus area or criteria relevant to the notification description.  
				- **NotificationTitle:** Title of the notification.  
				- **NotificationDescription:** One-sentence description in passive voice.  
				- **FullContent:** Analytical overview in HTML format.  

				## Instructions for Generative AI:
				- Use placeholders exactly as provided, ensuring consistency.  
				- Maintain a mild and suggestive tone throughout the output.  
				- Follow the structure and content guidelines strictly.  
				"""),
			((int)PromptCategory.LikelihoodWithPrevDataTemp,
				"""
				# Task  
				Write a weekly report on mobile sustainability using the specified format.
				
				# Response Format  
				
				- The content of the `Overview`, `ReportBreakdown`, and `ComputeBreakdown` property must be written in **Markdown style** but delivered as **HTML format**.  
				- Ensure the guidance is **clear, concise, and properly formatted**.  
				- Use only **black text**.  
				- Avoid using font sizes larger than `<h3>`.
				
				## Contents of the `Overview` property
				
				### General Guidance
				- The tone and response should adapt to the specific Sustainability Level of: `{SustainabilityLevel}`.  
				- Responses should always remain constructive, empathetic, and solution-oriented, but the focus will vary depending on the likelihood.  
				- Use appropriate emojis for the overview.
				- Use simple and easy to understand words.
				- Do not include title for the overview.

				---
				
				### Sustainability Level Guidance
				
				### **Crisis Level**
				- **Focus:** Immediate action. Acknowledge the urgency and provide critical next steps to address the situation.  
				- **Tone:** Serious yet hopeful. Stress the need for decisive action without being overly discouraging.  
				- **Example phrases:**  
				  - "This is a critical moment. We need to prioritize actions that will create an immediate impact."  
				  - "The current trajectory presents significant challenges, but it’s possible to turn things around with bold measures."  
				  - "Let’s identify the most urgent areas to address and act swiftly."  
				
				---
				
				### **Unsustainable**
				- **Focus:** Highlight areas for improvement and encourage focused efforts to reverse unsustainability.  
				- **Tone:** Determined and supportive. Acknowledge the difficulty while inspiring confidence in the ability to improve.  
				- **Example phrases:**  
				  - "There are clear signs of unsustainability, but targeted efforts can drive improvement."  
				  - "By addressing the core issues, we can begin to shift toward a more sustainable path."  
				  - "This is a challenging position, but there’s potential for progress with the right focus."  
				
				---
				
				### **Signs of Unsustainability**
				- **Focus:** Identify key areas where change can make the biggest impact. Build optimism for improvement.  
				- **Tone:** Constructive and encouraging. Acknowledge the warning signs and motivate action.  
				- **Example phrases:**  
				  - "There are signs that we need to make adjustments, and this is a great opportunity to do so."  
				  - "With focused effort, we can address these issues and start to see real progress."  
				  - "This is a critical moment for improvement—let’s work together to make an impact."  
				
				---
				
				### **Average Sustainability**
				- **Focus:** Recognize the potential to move from average to sustainable. Encourage efforts to break through barriers.  
				- **Tone:** Balanced and optimistic. Acknowledge progress while motivating further improvement.  
				- **Example phrases:**  
				  - "You’re on the right track, but there’s room to push further toward sustainability."  
				  - "This is a strong foundation to build on—let’s refine and enhance our approach."  
				  - "You’ve made steady progress; let’s focus on closing the gap to full sustainability."  
				
				---
				
				### **Close to Sustainable**
				- **Focus:** Celebrate progress while emphasizing the importance of maintaining and advancing efforts.  
				- **Tone:** Positive and motivating. Build confidence while inspiring continuous improvement.  
				- **Example phrases:**  
				  - "You’re so close to reaching sustainable levels—great work so far!"  
				  - "This progress is impressive, and with continued focus, you’ll achieve full sustainability."  
				  - "Let’s refine our efforts to ensure consistent and lasting success."  
				
				---
				
				### **Sustainable**
				- **Focus:** Acknowledge achievements and encourage consistent practices to maintain sustainability.  
				- **Tone:** Celebratory and supportive. Recognize the effort behind the success while inspiring further commitment.  
				- **Example phrases:**  
				  - "This is a fantastic accomplishment! Keep up the excellent work to sustain this progress."  
				  - "You’ve reached a sustainable level—let’s continue to build resilience and consistency."  
				  - "This is a testament to your dedication—keep striving for even greater heights."  
				
				---
				
				### **High Sustainability**
				- **Focus:** Celebrate and reinforce confidence. Highlight the importance of maintaining momentum and serving as a role model for success.  
				- **Tone:** Highly celebratory and motivational. Inspire pride in the achievement while encouraging continued commitment.  
				- **Example phrases:**  
				  - "Outstanding work! You’ve achieved high sustainability, and this sets a great example for others."  
				  - "This is a remarkable achievement—maintaining this level will ensure lasting success."  
				  - "Your efforts have paid off, and this is truly inspiring. Let’s keep leading the way!"  
				
				---
				
				## Tone Principles
				- **Empathetic:** Tailor responses to the emotional context of each likelihood.  
				- **Actionable:** Provide specific next steps, particularly at lower levels.  
				- **Motivational:** Inspire confidence and highlight the potential for progress or sustained success.
				
				---

				## Contents of the `ReportBreakdown` property

				- **Information: ** This section will help users know more about the weekly sustainability report. Help them understand the report and build have stronger foundation towards achieving sustainability.

				### **Current Sustainability Likelihood:**  
				   Emphasize the computed likelihood of sustainable mobile usage in a paragraph: `{CurrentSustainabilityLikelihood}`.

				### **Previous Week's Sustainability Likelihood:**  
				   Emphasize the previous week's computed likelihood of sustainable mobile usage in a paragraph: `{PreviousSustainabilityLikelihood}`.

				### **Comparison:**  
				   Compare previous week's results to the current week's result in a paragraph.

				### **Recommendations:**  
				   Suggest actionable steps to help the user maintain or improve their sustainability score in a paragraph. Only suggest actions related to Charging, Screen Time, and Use of WiFi instead of Mobile Data.
				

				## Contents of the `ComputeBreakdown` property

				### **How was this computed:**  
				    This is the computation for the current likelihood: `{CurrentLikelihoodComputation}`, also this is the computation for the previous week's likelihood: `{PreviousLikelihoodComputation}`. do not include this in the text, this is just for additinal context.  
				    Give a short paragraph explaining that the sustainability likelihood was computed using Bayesian Theorem, specifically to get the Posterior probabiltiy. It is then converted into Proportional probability.   
				"""),
			((int)PromptCategory.LikelihoodNoPrevDataTemp,
				$$"""
				# Task  
				Write a weekly report on mobile sustainability using the specified format.

				# Response Format

				- The content of the `Overview`, `ReportBreakdown`, and `ComputeBreakdown` property must be written in **Markdown style** but delivered as **HTML format**.  
				- Ensure the guidance is **clear, concise, and properly formatted**.  
				- Use only **black text**.  
				- Avoid using font sizes larger than `<h3>`.
				- Do not include percentage in the title.

				## Contents of the `Overview` property

				### General Guidance
				- The tone and response should adapt to the specific Sustainability Level of: `{SustainabilityLevel}`.  
				- Responses should always remain constructive, empathetic, and solution-oriented, but the focus will vary depending on the likelihood.  
				- Use appropriate emojis for the overview.
				- Use simple and easy to understand words.

				---
				
				### Sustainability Level Guidance
				
				### **Crisis Level**
				- **Focus:** Immediate action. Acknowledge the urgency and provide critical next steps to address the situation.  
				- **Tone:** Serious yet hopeful. Stress the need for decisive action without being overly discouraging.  
				- **Example phrases:**  
				  - "This is a critical moment. We need to prioritize actions that will create an immediate impact."  
				  - "The current trajectory presents significant challenges, but it’s possible to turn things around with bold measures."  
				  - "Let’s identify the most urgent areas to address and act swiftly."  
				
				---
				
				### **Unsustainable**
				- **Focus:** Highlight areas for improvement and encourage focused efforts to reverse unsustainability.  
				- **Tone:** Determined and supportive. Acknowledge the difficulty while inspiring confidence in the ability to improve.  
				- **Example phrases:**  
				  - "There are clear signs of unsustainability, but targeted efforts can drive improvement."  
				  - "By addressing the core issues, we can begin to shift toward a more sustainable path."  
				  - "This is a challenging position, but there’s potential for progress with the right focus."  
				
				---
				
				### **Signs of Unsustainability**
				- **Focus:** Identify key areas where change can make the biggest impact. Build optimism for improvement.  
				- **Tone:** Constructive and encouraging. Acknowledge the warning signs and motivate action.  
				- **Example phrases:**  
				  - "There are signs that we need to make adjustments, and this is a great opportunity to do so."  
				  - "With focused effort, we can address these issues and start to see real progress."  
				  - "This is a critical moment for improvement—let’s work together to make an impact."  
				
				---
				
				### **Average Sustainability**
				- **Focus:** Recognize the potential to move from average to sustainable. Encourage efforts to break through barriers.  
				- **Tone:** Balanced and optimistic. Acknowledge progress while motivating further improvement.  
				- **Example phrases:**  
				  - "You’re on the right track, but there’s room to push further toward sustainability."  
				  - "This is a strong foundation to build on—let’s refine and enhance our approach."  
				  - "You’ve made steady progress; let’s focus on closing the gap to full sustainability."  
				
				---
				
				### **Close to Sustainable**
				- **Focus:** Celebrate progress while emphasizing the importance of maintaining and advancing efforts.  
				- **Tone:** Positive and motivating. Build confidence while inspiring continuous improvement.  
				- **Example phrases:**  
				  - "You’re so close to reaching sustainable levels—great work so far!"  
				  - "This progress is impressive, and with continued focus, you’ll achieve full sustainability."  
				  - "Let’s refine our efforts to ensure consistent and lasting success."  
				
				---
				
				### **Sustainable**
				- **Focus:** Acknowledge achievements and encourage consistent practices to maintain sustainability.  
				- **Tone:** Celebratory and supportive. Recognize the effort behind the success while inspiring further commitment.  
				- **Example phrases:**  
				  - "This is a fantastic accomplishment! Keep up the excellent work to sustain this progress."  
				  - "You’ve reached a sustainable level—let’s continue to build resilience and consistency."  
				  - "This is a testament to your dedication—keep striving for even greater heights."  
				
				---
				
				### **High Sustainability**
				- **Focus:** Celebrate and reinforce confidence. Highlight the importance of maintaining momentum and serving as a role model for success.  
				- **Tone:** Highly celebratory and motivational. Inspire pride in the achievement while encouraging continued commitment.  
				- **Example phrases:**  
				  - "Outstanding work! You’ve achieved high sustainability, and this sets a great example for others."  
				  - "This is a remarkable achievement—maintaining this level will ensure lasting success."  
				  - "Your efforts have paid off, and this is truly inspiring. Let’s keep leading the way!"  
				
				---
				
				## Tone Principles
				- **Empathetic:** Tailor responses to the emotional context of each likelihood.  
				- **Actionable:** Provide specific next steps, particularly at lower levels.  
				- **Motivational:** Inspire confidence and highlight the potential for progress or sustained success.
				
				---

				## Contents of the `ReportBreakdown` property

				- **Information: ** This section will help users know more about the weekly sustainability report. Help them understand the report and build have stronger foundation towards achieving sustainability.

				### **Current Sustainability Likelihood:**  
				   Emphasize the computed likelihood of sustainable mobile usage in a paragraph: `{CurrentSustainabilityLikelihood}`.

				### **Recommendations:**  
				   Suggest actionable steps to help the user maintain or improve their sustainability score in a paragraph. Only suggest actions related to Charging, Screen Time, and Use of WiFi instead of Mobile Data.
				
				## Contents of the `ComputeBreakdown` property

				###. **How was this computed:**  
				   This is the computation for the current likelihood: `{CurrentLikelihoodComputation}`, do not include this in the text, this is just for additinal context.  
				   Give a short paragraph explaining that the sustainability likelihood was computed using Bayesian Theorem, specifically to get the Posterior probabiltiy. It is then converted into Proportional probability.
				"""),
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

		long countCheck = await db.ExecuteScalar<long>("SELECT COUNT(*) FROM TblPrompt");
		if (countCheck < prompts.Count)
		{
			// ensure that we have deleted incomplete data
			await db.ExecuteNonQuery("DELETE FROM TblPrompt");

			// insert prompts
			const string insertQuery = "INSERT INTO TblPrompt (Category, Content) VALUES (?, ?)";
			foreach (var prompt in prompts)
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

	public async Task PurgeData()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await db.ExecuteNonQuery("DROP TABLE TblPrompt");
	}
}
