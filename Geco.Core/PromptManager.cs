namespace Geco.Core
{
	public class PromptManager
	{
		private Dictionary<string, string> _prompts;

		public PromptManager() => _prompts = new Dictionary<string, string>
			{
				{"searchUserBased", "Based on the {userTopic}, generate three (3) responses and use a tone like that of a search engine."},
				{"searchCtgBased", "Using the tone of a search engine and based on the topic of {predefinedTopic}, generate three responses focusing on the {storedPromptRefinement}."},
				{"triggerNotif", "Given the unsustainable action based on {actionTrigger}, the user overstepped the sustainable baseline data of {sustainableBaselineData}. Give a short notification-like message focusing on {storedPromptRefinement}." },
				{"likelihoodWithPrevData","The current computed likelihood of sustainable use of mobile is {currentSustainabilityLikelihood}, the computation is as follows {currentLikelihoodComputation}. The values used are based on frequency, specifically: {currentFrequencyData}. The previous week computed likelihood of sustainable use of mobile is {previousSustainabilityLikelihood}, its computation is as follows {previousLikelihoodComputation}. The previous week computation made use of these frequencies: {previousFrequencyData}. Provide an analytical overview regarding the given sustainability data while including recommendations to improve involved sustainable practice. Also, if the previous week data is given, perform a comparison of the current and previous sustainability likelihood computation and value."},
				{"likelihoodNoPrevData", "The current computed likelihood of sustainable use of mobile is {currentSustainabilityLikelihood}, the computation is as follows {currentLikelihoodComputation}. The values used are based on frequency, specifically: {currentFrequencyData}. Provide an analytical overview regarding the given sustainability data while including recommendation to improve involved sustainable practice."}
			};

		public string GetSearchUserBasedPrompt(string userTopic) => FillPrompt("searchUserBased", new Dictionary<string, string>
			{
				{ "userTopic", userTopic }
			});

		public string GetSearchCtgBasedPrompt(string predefinedTopic, string storedPromptRefinement)
		{
			//TODO: Do a random selection of stored prompt refinement in the database, then use it as part of the prompt
			return FillPrompt("searchCtgBased", new Dictionary<string, string>
			{
				{ "predefinedTopic", predefinedTopic },
				{ "storedPromptRefinement", storedPromptRefinement }
			});
		}

		public string GetTriggerNotifPrompt(string actionTrigger, string sustainableBaselineData, string storedPromptRefinement)
		{
			//TODO: Do a random selection of stored prompt refinement in the database, then use it as part of the prompt
			return FillPrompt("triggerNotif", new Dictionary<string, string>
			{
				{ "actionTrigger", actionTrigger },
				{ "sustainableBaselineData", sustainableBaselineData },
				{ "storedPromptRefinement", storedPromptRefinement }
			});
		}

		public string GetSustLikelihoodPrompt(string currentSustainabilityLikelihood, string currentLikelihoodComputation, string currentFrequencyData) => FillPrompt("likelihoodNoPrevData", new Dictionary<string, string>
			{
				{ "currentSustainabilityLikelihood", currentSustainabilityLikelihood },
				{ "currentLikelihoodComputation", currentLikelihoodComputation },
				{ "currentFrequencyData", currentFrequencyData }
			});

		public string GetSustLikelihoodPrompt(string currentSustainabilityLikelihood, string currentLikelihoodComputation, string currentFrequencyData, string previousSustainabilityLikelihood, string previousLikelihoodComputation, string previousFrequencyData) => FillPrompt("likelihoodWithPrevData", new Dictionary<string, string>
			{
				{ "currentSustainabilityLikelihood", currentSustainabilityLikelihood },
				{ "currentLikelihoodComputation", currentLikelihoodComputation },
				{ "currentFrequencyData", currentFrequencyData },
				{ "previousSustainabilityLikelihood", previousSustainabilityLikelihood },
				{ "previousLikelihoodComputation", previousLikelihoodComputation },
				{ "previousFrequencyData", previousFrequencyData }
			});

		private string FillPrompt(string key, Dictionary<string, string> data)
		{
			if (!_prompts.TryGetValue(key, out string? prompt))
			{
				return "Prompt not found.";
			}

			foreach (var placeholder in data)
			{
				string placeholderKey = "{" + placeholder.Key + "}";
				prompt = prompt.Replace(placeholderKey, placeholder.Value);
			}

			return prompt;
		}
	}
}
