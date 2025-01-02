namespace Geco;


public class GecoSettings
{
	public static bool DarkMode
	{
		get => Preferences.Default.Get(nameof(DarkMode), false);
		set => Preferences.Default.Set(nameof(DarkMode), value);
	}

	public static bool Monitor
	{
		get => Preferences.Default.Get(nameof(Monitor), false);
		set => Preferences.Default.Set(nameof(Monitor), value);
	}

	public static bool Notifications
	{
		get => Preferences.Default.Get(nameof(Notifications), false);
		set => Preferences.Default.Set(nameof(Notifications), value);
	}

	public static DateTime WeeklyReportDateTime
	{
		get => Preferences.Default.Get(nameof(WeeklyReportDateTime), DateTime.UnixEpoch);
		set => Preferences.Default.Set(nameof(WeeklyReportDateTime), value);
	}

	public static DateTime DailyReportDateTime
	{
		get => Preferences.Default.Get(nameof(DailyReportDateTime), DateTime.UnixEpoch);
		set => Preferences.Default.Set(nameof(DailyReportDateTime), value);
	}
}
