namespace Geco.Core.Database;

/// <summary>
///     Represents the device interaction trigger type.
///     Negative values indicate unsustainable actions,
///     positive values indicate sustainable actions.
/// </summary>
public enum DeviceInteractionTrigger
{
	NetworkUsageUnsustainable = -3,
	DeviceUsageUnsustainable,
	ChargingUnsustainable,
	None, // DO NOT USE
	ChargingSustainable,
	DeviceUsageSustainable,
	NetworkUsageSustainable
}
