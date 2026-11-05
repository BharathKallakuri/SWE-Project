using Insights.AI;
using Xunit;

namespace Insights.Test;

public class InsightsManagerTests
{
    [Fact]
    public async Task RequestTextAnalysisAsync_UsesActiveProvider()
    {
        var provider = new FakeInsightsProvider("TestModel");
        var manager = new InsightsManager(provider);

        string result = await manager.RequestTextAnalysisAsync("Test incident");

        Assert.Equal("Text result from TestModel", result);
        Assert.Equal("Test incident", provider.LastText);
    }

    [Fact]
    public async Task RequestImageAnalysisAsync_UsesActiveProvider()
    {
        var provider = new FakeInsightsProvider("TestModel");
        var manager = new InsightsManager(provider);

        byte[] image = { 1, 2, 3 };
        string prompt = "Analyse this image";

        string result = await manager.RequestImageAnalysisAsync(image, prompt);

        Assert.Equal("Image result from TestModel", result);
        Assert.Same(image, provider.LastImage);
        Assert.Equal(prompt, provider.LastPrompt);
    }

    [Fact]
    public async Task SwitchModel_UsesNewProvider()
    {
        var firstProvider = new FakeInsightsProvider("FirstModel");
        var secondProvider = new FakeInsightsProvider("SecondModel");

        var manager = new InsightsManager(firstProvider);

        manager.SwitchModel(secondProvider);

        string result = await manager.RequestTextAnalysisAsync("Test incident");

        Assert.Equal("Text result from SecondModel", result);
        Assert.Equal(0, firstProvider.TextCallCount);
        Assert.Equal(1, secondProvider.TextCallCount);
    }

    private class FakeInsightsProvider : IInsightsProvider
    {
        public string ModelName { get; }

        public string? LastText { get; private set; }
        public byte[]? LastImage { get; private set; }
        public string? LastPrompt { get; private set; }

        public int TextCallCount { get; private set; }

        public FakeInsightsProvider(string modelName)
        {
            ModelName = modelName;
        }

        public Task<string> AnalyzeTrafficIncidentAsync(string textDescription)
        {
            LastText = textDescription;
            TextCallCount++;

            return Task.FromResult($"Text result from {ModelName}");
        }

        public Task<string> AnalyzeImageAsync(byte[] imageData, string prompt)
        {
            LastImage = imageData;
            LastPrompt = prompt;

            return Task.FromResult($"Image result from {ModelName}");
        }
    }
}
