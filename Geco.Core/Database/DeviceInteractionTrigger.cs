namespace Geco.Core.Database;

/// <summary>
///     Represents the device interaction trigger type.
///     Negative values indicate unsustainable actions,
///     positive values indicate sustainable actions.
/// </summary>
public enum DeviceInteractionTrigger
{
	LocationUsageUnsustainable = -4,
	NetworkUsageUnsustainable,
	DeviceUsageUnsustainable,
	ChargingUnsustainable,
	None, // DO NOT USE
	ChargingSustainable,
	DeviceUsageSustainable,
	NetworkUsageSustainable,
	LocationUsageSustainable
}
