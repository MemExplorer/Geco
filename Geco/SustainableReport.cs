﻿using System.Globalization;
using System.Text;
using System.Text.Json;
using Geco.Core;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Notification;
using GoogleGeminiSDK;
using Markdig;

namespace Geco;

public class SustainableReport
{
	public static async Task<(WeeklyReportContent ReportDetails, string FullReportContent)?> CreateWeeklyReport()
	{
		var constructedPrompt = await ConstructLikelihoodPrompt();
		if (constructedPrompt == null)
		{
			GlobalContext.Logger.Info<SustainableReport>(
				"Warning: Unable to generate the weekly report because one of the attributes has a value of zero.");
			return null;
		}

		string likelihoodPrompt = constructedPrompt.GetValueOrDefault().Item1;
		string htmlTemplate = constructedPrompt.GetValueOrDefault().Item2;
		try
		{
			var geminiClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
			var geminiSettings =
				GlobalContext.Services.GetKeyedService<GeminiSettings>(GlobalContext.GeminiWeeklyReport);
			WeeklyReportContent? reportContent = null;
			await Utils.RetryAsyncTaskOrThrow<TaskCanceledException>(3, async () =>
			{
				var weeklyReportResponse = await geminiClient.SendMessage(likelihoodPrompt, settings: geminiSettings);
				var deserializedWeeklyReport =
					JsonSerializer.Deserialize<List<WeeklyReportContent>>(weeklyReportResponse.Text!)!;
				reportContent = deserializedWeeklyReport.First();
			});

			// ensure weekly report is not null
			if (reportContent == null)
				throw new Exception("Weekly report is null!");

			// replace placeholders
			var mdPipeline = GlobalContext.Services.GetRequiredService<MarkdownPipeline>();
			htmlTemplate = StringHelpers.FormatString(htmlTemplate,
				new
				{
					Overview = Markdown.ToHtml(reportContent.Overview, mdPipeline),
					ReportBreakdown = Markdown.ToHtml(reportContent.ReportBreakdown, mdPipeline),
					ComputeBreakdown = Markdown.ToHtml(reportContent.ComputeBreakdown, mdPipeline)
				});

			return (reportContent, htmlTemplate);
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<SustainableReport>(ex,
				"Weekly report creation resulted into an error.");
		}

		return null;
	}

	private static async Task<(string, string)?> ConstructLikelihoodPrompt()
	{
		// fetch trigger records for current week
		var triggerRepo = GlobalContext.Services.GetRequiredService<TriggerRepository>();
		var promptRepo = GlobalContext.Services.GetRequiredService<PromptRepository>();

		await using var weeklyReportTemplateStream =
			await FileSystem.OpenAppPackageFileAsync("WeeklyReportTemplate.html");
		using var weeklyReportStreamReader = new StreamReader(weeklyReportTemplateStream);
		string htmlTemplate = await weeklyReportStreamReader.ReadToEndAsync();

		// current bayes computation
		var currentWeekTriggerRecords = (await triggerRepo.FetchWeekOneTriggerRecords()).ToDictionary();
		var currentWeekBayesInstance = BayesInstanceFromRecords(currentWeekTriggerRecords);
		if (currentWeekBayesInstance == null)
			return null;

		var currentWeekPercentage = currentWeekBayesInstance.Compute();
		var currentWeekComputationSolution = currentWeekBayesInstance.GetComputationSolution();
		double currentWeekProbabilityRounded = Math.Round(currentWeekPercentage.PositiveProbability, 2);
		(string sustainabilityLevel, string gecoEmojiPath) = currentWeekProbabilityRounded switch
		{
			>= 90 => ("High Sustainability", "1_HighlySustainable.png"),
			>= 75 and < 90 => ("Sustainable", "2_Sustainable.png"),
			>= 60 and < 75 => ("Close to Sustainable", "3_ClosetoSustainable.png"),
			>= 45 and < 60 => ("Average Sustainability", "4_AverageSustainability.png"),
			>= 30 and < 45 => ("Signs of Unsustainability", "5_SignsofUnsustainability.png"),
			>= 15 and < 30 => ("Unsustainable", "6_Unsustainable.png"),
			_ => ("Crisis level", "7_CrisisLevel.png")
		};

		await using var gecoEmoji = await FileSystem.OpenAppPackageFileAsync("GecoEmojis/" + gecoEmojiPath);
		using var gecoEmojiStream = new MemoryStream();
		await gecoEmoji.CopyToAsync(gecoEmojiStream);
		byte[] gecoEmojiBytes = gecoEmojiStream.ToArray();

		// set values for current week data
		string base64Image = "data:image/png;base64," + Convert.ToBase64String(gecoEmojiBytes);
		htmlTemplate = StringHelpers.FormatString(htmlTemplate,
			new
			{
				CurrentWeekPercentage = currentWeekProbabilityRounded.ToString(CultureInfo.InvariantCulture),
				CurrentWeekTableFrequency = BayesFrequencyToJavascript(currentWeekBayesInstance.GetFrequencyData()),
				EmojiPlaceholder = base64Image
			});

		// check if we have data from last 2 weeks
		if (await triggerRepo.HasHistory())
		{
			// fetch data from last 2 weeks
			var previousWeekTriggerRecords = (await triggerRepo.FetchWeekTwoTriggerRecords()).ToDictionary();
			var previousWeekBayesInstance = BayesInstanceFromRecords(previousWeekTriggerRecords);
			if (previousWeekBayesInstance != null)
			{
				// previous week bayes computation
				var previousWeekPercentage = previousWeekBayesInstance.Compute();
				var previousWeekComputationSolution = previousWeekBayesInstance.GetComputationSolution();
				double previousWeekProbabilityRounded = Math.Round(previousWeekPercentage.PositiveProbability, 2);

				// set values for previous week data
				htmlTemplate = StringHelpers.FormatString(htmlTemplate,
					new
					{
						PreviousWeekPercentage = previousWeekProbabilityRounded.ToString(CultureInfo.InvariantCulture),
						PreviousWeekTableFrequency =
							BayesFrequencyToJavascript(previousWeekBayesInstance.GetFrequencyData())
					});

				// build likelihood prompt
				return (await promptRepo.GetLikelihoodWithHistoryPrompt(
					currentWeekProbabilityRounded, currentWeekComputationSolution.PositiveComputation,
					previousWeekProbabilityRounded, previousWeekComputationSolution.PositiveComputation,
					sustainabilityLevel), htmlTemplate);
			}
		}

		// replace previous week data with null
		htmlTemplate = StringHelpers.FormatString(htmlTemplate,
			new { PreviousWeekPercentage = "null", PreviousWeekTableFrequency = "null" });

		return (
			await promptRepo.GetLikelihoodPrompt(currentWeekProbabilityRounded,
				currentWeekComputationSolution.PositiveComputation, sustainabilityLevel), htmlTemplate);
	}

	private static string BayesFrequencyToJavascript(IDictionary<string, BayesTheoremAttribute> frequencyData)
	{
		var sb = new StringBuilder();
		sb.Append('{');
		foreach (var entry in frequencyData)
		{
			sb.Append('"');
			sb.Append(entry.Key);
			sb.Append("\":");

			sb.Append('[');
			sb.Append(entry.Value.Positive);
			sb.Append(',');
			sb.Append(entry.Value.Negative);
			sb.Append("],");
		}

		sb.Append("};");

		return sb.ToString();
	}

	private static BayesTheorem? BayesInstanceFromRecords(
		Dictionary<DeviceInteractionTrigger, int> currentWeekTriggerRecords)
	{
		int chargingPositive =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.ChargingSustainable, 0);
		int chargingNegative =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.ChargingUnsustainable, 0);
		int networkUsagePositive =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.NetworkUsageSustainable, 0);
		int networkUsageNegative =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.NetworkUsageUnsustainable, 0);
		int deviceUsagePositive =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.DeviceUsageSustainable, 0);
		int deviceUsageNegative =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.DeviceUsageUnsustainable, 0);

		if (chargingPositive == 0 && chargingNegative == 0)
			return null;

		if (networkUsagePositive == 0 && networkUsageNegative == 0)
			return null;

		if (deviceUsagePositive == 0 && deviceUsageNegative == 0)
			return null;

		// pass frequency data to Bayes Math Model
		var bayesInst = new BayesTheorem();
		bayesInst.AppendData("Charging", chargingPositive, chargingNegative);
		bayesInst.AppendData("Network Usage", networkUsagePositive, networkUsageNegative);
		bayesInst.AppendData("Device Usage", deviceUsagePositive, deviceUsageNegative);
		return bayesInst;
	}
}
