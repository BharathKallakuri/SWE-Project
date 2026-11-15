using Google.GenAI.Types;

namespace Insights.AI;

public interface ILogAnalysisProvider
{
    string ModelName { get; }

    Task<string?> GenerateStructuredContentAsync(
        string model,
        string prompt,
        GenerateContentConfig config);
}
