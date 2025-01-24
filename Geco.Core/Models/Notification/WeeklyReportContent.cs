namespace Geco.Core.Models.Notification;

public record WeeklyReportContent(
	string NotificationTitle,
	string NotificationDescription,
	string Overview,
	string ReportBreakdown);
