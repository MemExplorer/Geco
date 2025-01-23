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
				PredefinedTopic = $"Sustainable {predefinedTopic}", StoredPromptRefinement = randomPromptRefinement
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
		string currentLikelihoodComputation,
		string currentFrequencyData) => await BuildPrompt(PromptCategory.LikelihoodNoPrevDataTemp,
		new
		{
			CurrentSustainabilityLikelihood =
				currentSustainabilityLikelihood.ToString(CultureInfo.InvariantCulture) + "%",
			CurrentLikelihoodComputation = currentLikelihoodComputation,
			CurrentFrequencyData = currentFrequencyData,
			StatusIcon = ""
		});

	public async Task<string> GetLikelihoodWithHistoryPrompt(double currentSustainabilityLikelihood,
		string currentLikelihoodComputation,
		string currentFrequencyData, double previousSustainabilityLikelihood, string previousLikelihoodComputation,
		string previousFrequencyData)
	{
		const string upArrowSvg = """
		                          <svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' version='1.1' width='20' height='20' viewBox='1 1 256 256' xml:space='preserve'>
		                          <defs>
		                          </defs>
		                          <g style='stroke: none; stroke-width: 0; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: none; fill-rule: nonzero; opacity: 1;' transform='translate(1.4065934065934016 1.4065934065934016) scale(2.81 2.81)' >
		                          <path d='M 43.779 0.434 L 12.722 25.685 c -0.452 0.368 -0.714 0.92 -0.714 1.502 v 19.521 c 0 0.747 0.43 1.427 1.104 1.748 c 0.674 0.321 1.473 0.225 2.053 -0.246 L 45 23.951 l 29.836 24.258 c 0.579 0.471 1.378 0.567 2.053 0.246 c 0.674 -0.321 1.104 -1.001 1.104 -1.748 V 27.187 c 0 -0.582 -0.263 -1.134 -0.714 -1.502 L 46.221 0.434 C 45.51 -0.145 44.49 -0.145 43.779 0.434 z' style='stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: rgb(39,193,39); fill-rule: nonzero; opacity: 1;' transform=' matrix(1 0 0 1 0 0) ' stroke-linecap='round' />
		                          <path d='M 43.779 41.792 l -31.057 25.25 c -0.452 0.368 -0.714 0.919 -0.714 1.502 v 19.52 c 0 0.747 0.43 1.427 1.104 1.748 c 0.674 0.321 1.473 0.225 2.053 -0.246 L 45 65.308 l 29.836 24.258 c 0.579 0.471 1.378 0.567 2.053 0.246 c 0.674 -0.321 1.104 -1.001 1.104 -1.748 V 68.544 c 0 -0.583 -0.263 -1.134 -0.714 -1.502 l -31.057 -25.25 C 45.51 41.214 44.49 41.214 43.779 41.792 z' style='stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: rgb(39,193,39); fill-rule: nonzero; opacity: 1;' transform=' matrix(1 0 0 1 0 0) ' stroke-linecap='round' />
		                          </g>
		                          </svg>
		                          """;

		const string downArrowSvg = """
		                            <svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' version='1.1' width='20' height='20' viewBox='0 0 256 256' xml:space='preserve'>
		                            <defs>
		                            </defs>
		                            <g style='stroke: none; stroke-width: 0; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: none; fill-rule: nonzero; opacity: 1;' transform='translate(1.4065934065934016 1.4065934065934016) scale(2.81 2.81)' >
		                            <path d='M 43.779 89.566 L 12.722 64.315 c -0.452 -0.368 -0.714 -0.92 -0.714 -1.502 V 43.293 c 0 -0.747 0.43 -1.427 1.104 -1.748 c 0.674 -0.321 1.473 -0.225 2.053 0.246 L 45 66.049 l 29.836 -24.258 c 0.579 -0.471 1.378 -0.567 2.053 -0.246 c 0.674 0.321 1.104 1.001 1.104 1.748 v 19.521 c 0 0.582 -0.263 1.134 -0.714 1.502 L 46.221 89.566 C 45.51 90.145 44.49 90.145 43.779 89.566 z' style='stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: rgb(206,62,62); fill-rule: nonzero; opacity: 1;' transform=' matrix(1 0 0 1 0 0) ' stroke-linecap='round' />
		                            <path d='M 43.779 48.208 l -31.057 -25.25 c -0.452 -0.368 -0.714 -0.919 -0.714 -1.502 V 1.936 c 0 -0.747 0.43 -1.427 1.104 -1.748 c 0.674 -0.321 1.473 -0.225 2.053 0.246 L 45 24.692 L 74.836 0.434 c 0.579 -0.471 1.378 -0.567 2.053 -0.246 c 0.674 0.321 1.104 1.001 1.104 1.748 v 19.521 c 0 0.583 -0.263 1.134 -0.714 1.502 l -31.057 25.25 C 45.51 48.786 44.49 48.786 43.779 48.208 z' style='stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: rgb(206,62,62); fill-rule: nonzero; opacity: 1;' transform=' matrix(1 0 0 1 0 0) ' stroke-linecap='round' />
		                            </g>
		                            </svg>
		                            """;

		const string tildeSvg = """
		                        <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 50' width='20' height='50'>
		                        	<path d='M1 25 Q 25 5, 50 25 T 90 25' 
		                        			fill='none' 
		                        			stroke='gray' 
		                        			stroke-width='15' />
		                        </svg>
		                        """;

		string statusIcon = currentFrequencyData.CompareTo(previousSustainabilityLikelihood) switch
		{
			-1 => downArrowSvg,
			1 => upArrowSvg,
			_ => tildeSvg
		};

		return await BuildPrompt(PromptCategory.LikelihoodWithPrevDataTemp,
			new
			{
				CurrentSustainabilityLikelihood =
					currentSustainabilityLikelihood.ToString(CultureInfo.InvariantCulture) + "%",
				CurrentLikelihoodComputation = currentLikelihoodComputation,
				CurrentFrequencyData = currentFrequencyData,
				PreviousSustainabilityLikelihood =
					previousSustainabilityLikelihood.ToString(CultureInfo.InvariantCulture) + "%",
				PreviousLikelihoodComputation = previousLikelihoodComputation,
				PreviousFrequencyData = previousFrequencyData,
				StatusIcon = statusIcon
			});
	}

	protected override async Task InitializeTables()
	{
		await base.InitializeTables();
		await AddInitialPrompts();
	}

	private async Task AddInitialPrompts()
	{
		using var db = await SqliteDb.GetTransient(DatabaseDir);
		const string weeklyReportTemplate = """
		                                    <html>
		                                    	<head>
		                                    		<style>
		                                    		body {
		                                    			font-family: Arial, sans-serif;
		                                    			display: flex;
		                                    			flex-direction: column;
		                                    			align-items: center;
		                                    			padding: 20px;
		                                    			margin: 0;
		                                    		}
		                                    
		                                    		.circle {
		                                    			position: relative;
		                                    			width: 100px;
		                                    			height: 100px;
		                                    			border-radius: 50%;
		                                    			background: conic-gradient(var(--color, red) 0%, var(--color, red) var(--percentage, 0%), #ccc var(--percentage, 0%));
		                                    			display: flex;
		                                    			justify-content: center;
		                                    			align-items: center;
		                                    			box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
		                                    			margin-right: 20px;
		                                    		}
		                                    
		                                    		.circle::before {
		                                    			content: '';
		                                    			position: absolute;
		                                    			width: 80px;
		                                    			height: 80px;
		                                    			background-color: #f4f4f9;
		                                    			border-radius: 50%;
		                                    		}
		                                    
		                                    		.circle .grid-inner {
		                                    			position: absolute;
		                                    			color: #333;
		                                    			align-items:center;
		                                    			justify-content:center;
		                                    			display:flex;
		                                    		}
		                                    		
		                                    		.circle .grid-inner svg {
		                                    			padding-bottom: 2px;
		                                    		}
		                                    
		                                    		.overview {
		                                    			margin-bottom: 20px;
		                                    		}
		                                    
		                                    		.collapsible {
		                                    			max-width: 150px;
		                                    			background-color: #039967;
		                                    			color: white;
		                                    			border: none;
		                                    			outline: none;
		                                    			align-self: start;
		                                    			padding: 7px 10px;
		                                    			cursor: pointer;
		                                    			border-radius: 5px;
		                                    			box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
		                                    		}
		                                    
		                                    		.collapsible:hover {
		                                    			background-color: #026343;
		                                    		}
		                                    
		                                    		.content {
		                                    			margin-top: 5px;
		                                    			display: none;
		                                    			overflow: hidden;
		                                    		}
		                                    		
		                                    		.flex-container {
		                                    			display: flex;
		                                    			flex-wrap:wrap;
		                                    		}
		                                    		.title {
		                                    			height: auto;
		                                    			align-self:center;
		                                    		}
		                                    		</style>
		                                    	</head>
		                                    	<body>
		                                    		<div class='flex-container'>
		                                    			<div class='circle' id='percentageCircle'>
		                                    			<div class='grid-inner'>
		                                    				<h4 id='percentageValue'></h4>
		                                    				{StatusIcon}
		                                    			</div>
		                                    			</div>
		                                    			
		                                    			<div class='title'>
		                                    				<h3>Weekly<br>Sustainability<br>Likelihood Report</h3>
		                                    			</div>
		                                    		</div>
		                                    		
		                                    		<div class='overview'>
		                                    			<p><!-- Insert the 1 paragraph overview here --></p>
		                                    		</div>
		                                    		<button class='collapsible'>Find out more</button>
		                                    		<div class='content' id='collapsibleContent'>
		                                    			<p><!-- Insert the full breakdown here --></p>
		                                    		</div>
		                                    		<script>
		                                    		const collapsible = document.querySelector('.collapsible');
		                                    		const collapsibleContent = document.querySelector('.content');
		                                    		collapsible.addEventListener('click', () => {
		                                    			const isExpanded = collapsibleContent.style.display === 'block';
		                                    			collapsibleContent.style.display = isExpanded ? 'none' : 'block';
		                                    			collapsibleContent.style.color = document.body.style.color;
		                                    			collapsible.remove();
		                                    		});
		                                    
		                                    		function calculateColor(percentage) {
		                                    			let red, green;
		                                    			if (percentage <= 50) {
		                                    			red = 255;
		                                    			green = Math.round(percentage * 5.1);
		                                    			} else {
		                                    			red = Math.round((100 - percentage) * 5.1);
		                                    			green = 255;
		                                    			}
		                                    			return `rgb(${red}, ${green}, 0)`;
		                                    		}
		                                    		const percentage = 0;
		                                    		const circle = document.getElementById('percentageCircle');
		                                    		const percentageValue = document.getElementById('percentageValue');
		                                    		circle.style.setProperty('--color', calculateColor(percentage));
		                                    		circle.style.setProperty('--percentage', `${percentage}%`);
		                                    		percentageValue.textContent = `${percentage}%`;
		                                    		</script>
		                                    	</body>
		                                    </html>
		                                    """;

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
				$$"""
				# Sustainability Analysis for Mobile Use

				## 1. Overview of Current Sustainability Likelihood:
				- **Current Sustainability Likelihood:**  
				  Provide the current computed likelihood of sustainable mobile use: `{CurrentSustainabilityLikelihood}`.

				- **Current Likelihood Posterior Computation:**  
				  Detail the computation for the current likelihood: `{CurrentLikelihoodComputation}`. Emphasize that this is the posterior computation, the current sustainability likelihood percetange result shown is the converted propotional probability.

				- **Frequency Data (Current):**  
				  Include the frequency data used for the computation: `{CurrentFrequencyData}`.

				## 2. Overview of Previous Sustainability Likelihood:
				- **Previous Sustainability Likelihood:**  
				  Provide the previous week's computed likelihood of sustainable mobile use: `{PreviousSustainabilityLikelihood}`.

				- **Previous Likelihood Posterior Computation:**  
				  Detail the computation for the previous likelihood: `{PreviousLikelihoodComputation}`. Emphasize that this is the posterior computation, the previous sustainability likelihood percetange result shown is the converted propotional probability.

				- **Frequency Data (Previous):**  
				  Include the frequency data used for the previous week's computation: `{PreviousFrequencyData}`.

				## 3. FullContent Property:
				- **Detailed HTML Analytical Overview:**  
				  Strictly use the following HTML below for the `FullContent` property. Insert a summary of the report in the div with the class `overview`, and insert the full discussion, in-depth analysis, and comparison of previous and current sustainability likelihood with headings in each paragraph (do not use the tag <code> and <br> after a heading), and organized structure in the div with the class `content`. Again, strictly use the HTML template and the styling, change only the constant percentage value in the JavaScript, and only populate the divs `overview` and `content` as instructed earlier. Use simple to understand words on the overview that any user can understand:

				```
				{{weeklyReportTemplate}}
				```
				"""),
			((int)PromptCategory.LikelihoodNoPrevDataTemp,
				$$"""
				# Sustainability Analysis for Mobile Use

				## 1. Overview of Current Sustainability Likelihood:
				- **Current Sustainability Likelihood:**  
				  Provide the current computed likelihood of sustainable mobile use: `{CurrentSustainabilityLikelihood}`.

				- **Current Likelihood Posterior Computation:**  
				  Detail the computation for the current likelihood: `{CurrentLikelihoodComputation}`. Emphasize that this is the posterior computation, the current sustainability likelihood percetange result shown is the converted propotional probability.

				- **Frequency Data (Current):**  
				  Include the frequency data used for the computation: `{CurrentFrequencyData}`.

				## 2. FullContent Property:
				- **Detailed HTML Analytical Overview:**  
				  Strictly use the following HTML below as the template for the `FullContent` property. Insert a summary of the report in the div with the class `overview`, and insert the full discussion and in-depth analysis of the sustainability likelihood with headings in each paragraph (do not use the tag <code> and <br> after a heading), and organized structure in the div with the class `content`. Again, strictly use the HTML template and the styling, change only the constant percentage value in JavaScript, and only populate the divs `overview` and `content` as instructed earlier. Use simple to understand words on the overview that any user can understand:

				```
				{{weeklyReportTemplate}}
				```
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
