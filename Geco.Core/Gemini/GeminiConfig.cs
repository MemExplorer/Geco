using Geco.Core.Gemini.Rest.Models.Message;

namespace Geco.Core.Gemini;

/// <summary>
/// Gemini Client Configuration
/// </summary>
public class GeminiConfig()
{
	/// <summary>
	/// Determine whether it is necessary to save the conversation or not
	/// </summary>
	public bool Conversational { get; init; } = false;

	/// <summary>
	/// System instructions are part of your overall prompts
	/// </summary>
	public string? SystemInstructions { get; init; } = null;

	/// <summary>
	/// Role can be either model or user
	/// </summary>
	public string? Role { get; init; } = null;

	/// <summary>
	/// Configuration options for model generation and outputs
	/// </summary>
	public GenerationConfig? GenerationConfig { get; init; } = null;
}
