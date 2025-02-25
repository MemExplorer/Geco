namespace Geco.Core.Models.ActionObserver;

/// <summary>
///     Represents the device interaction trigger type.
///     Negative values indicate unsustainable actions,
///     positive values indicate sustainable actions.
/// </summary>
public enum DeviceInteractionTrigger
{
	BrowserUsageUnsustainable = -5,
	LocationUsageUnsustainable,
	NetworkUsageUnsustainable,
	DeviceUsageUnsustainable,
	ChargingUnsustainable,
	None, // DO NOT USE
	ChargingSustainable,
	DeviceUsageSustainable,
	NetworkUsageSustainable,
	LocationUsageSustainable,
	BrowserUsageSustainable
}
