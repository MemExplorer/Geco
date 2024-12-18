namespace Geco.Core.Models.Prompt;

/// <summary>
///     Represents the prompt category.
/// </summary>
public enum PromptCategory
{
	SearchUserBasedTemp,
	SearchCtgBasedTemp,
	TriggerNotifTemp,
	LikelihoodWithPrevDataTemp,
	LikelihoodNoPrevDataTemp,
	EnergySearchRefinement,
	WasteSearchRefinement,
	FashionSearchRefinement,
	TransportSearchRefinement,
	ChargingRefinement,
	DeviceUsageRefinement,
	NetworkUsageRefinement,
	LocationServicesRefinement,
	SearchingBrowserRefinement
}
