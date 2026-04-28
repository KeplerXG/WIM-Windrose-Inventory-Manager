using System;
using System.Text;

namespace WIM;

/// <summary>
/// Attribution for this build of the editor (also mirrored in assembly metadata).
/// </summary>
internal static class Credits
{
	public const string AppDisplayName = "WIM - Windrose Inventory Manager";

	public const string AppVersionDisplay = "2.0 Beta";

	public const string WindowTitle = AppDisplayName + "  v" + AppVersionDisplay;

	public const string Author = "KeplerXG";

	/// <summary>Release / attribution date for this fork.</summary>
	public const string CreatedDateDisplay = "19 April 2026";

	public const string AttributionLine = "Created by KeplerXG · " + CreatedDateDisplay;

	/// <summary>
	/// Comma-separated display names for the credits dialog (The Vortex Discord testers). Leave empty for a general thanks.
	/// </summary>
	public const string VortexDiscordTestersCsv = "";

	internal static string BuildCreditsBody()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(AppDisplayName);
		sb.Append("Version ").AppendLine(AppVersionDisplay);
		sb.AppendLine();
		sb.AppendLine("Author");
		sb.AppendLine(Author);
		sb.AppendLine();
		sb.AppendLine("Date built");
		sb.AppendLine(CreatedDateDisplay);
		sb.AppendLine();
		sb.Append(FormatVortexThanks());
		return sb.ToString().TrimEnd();
	}

	internal static string FormatVortexThanks()
	{
		string[] parts = VortexDiscordTestersCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return "Thanks to The Vortex Discord community for testing and feedback.";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Thanks to these members of The Vortex Discord for testing:");
		foreach (string p in parts)
		{
			sb.AppendLine("  • " + p);
		}
		return sb.ToString().TrimEnd();
	}
}
