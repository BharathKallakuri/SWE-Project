namespace Insights.AI;

public class InsightsManager
{
    private IInsightsProvider _activeProvider;

    public InsightsManager(IInsightsProvider defaultProvider)
    {
        _activeProvider = defaultProvider;
    }

    public void SwitchModel(IInsightsProvider newProvider)
    {
        _activeProvider = newProvider;
    }

    public async Task<string> RequestTextAnalysisAsync(string text)
    {
        return await _activeProvider.AnalyzeTrafficIncidentAsync(text);
    }

    public async Task<string> RequestImageAnalysisAsync(
        byte[] image,
        string prompt)
    {
        return await _activeProvider.AnalyzeImageAsync(image, prompt);
    }
}
