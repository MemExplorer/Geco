using Geco.Core.Gemini.Rest;
using Geco.Core.Gemini.Rest.Models.Message;
using System.Diagnostics;
using Xunit;

namespace Geco.Core.Test;

public class GeminiTests
{
    GeminiRestClient GeminiRC = new GeminiRestClient(Secrets.GEMINI_API_KEY);

    [Fact]
    public async Task TestWithHistory()
    {
        Debug.WriteLine("Test with History:");
        List<MessageContent> messages = new List<MessageContent>();
        await GeminiRC.TextPrompt("What is 1 + 1?", messages);
        await GeminiRC.TextPrompt("Are you sure?", messages);

        var dialogMsgs = String.Join("\n", messages.Select(x => x.Role + ": " + x.ExtractMessage()));
        Debug.WriteLine(dialogMsgs);
    }

    [Fact]
    public async Task TestWithoutHistory()
    {
        Debug.WriteLine("Test without History:");
        List<MessageContent> backupHistory = new List<MessageContent>();
        List<MessageContent> messages = new List<MessageContent>();
        await GeminiRC.TextPrompt("What is 1 + 1?", messages);
        backupHistory.AddRange(messages);
        messages = new List<MessageContent>();
        await GeminiRC.TextPrompt("Are you sure?", messages);
        backupHistory.AddRange(messages);

        var dialogMsgs = String.Join("\n", backupHistory.Select(x => x.Role + ": " + x.ExtractMessage()));
        Debug.WriteLine(dialogMsgs);
    }
}
