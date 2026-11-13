namespace Insights.AI;

public interface IInsightsProvider
{
    string ModelName { get; }

    Task<string> AnalyzeTrafficIncidentAsync(string textDescription);

    Task<string> AnalyzeImageAsync(byte[] imageData, string prompt);

    Task<string> GenerateChatResponseAsync(
        string userMessage,
        string? chatContext = null);
}
