using Google.GenAI;
using Google.GenAI.Types;
using System.Collections.Generic;

namespace Insights.AI;

public class GeminiProvider : IInsightsProvider
{
    private Client _geminiClient = null!;

    public string ModelName => "gemini-3.5-flash";

    private GeminiProvider() { }

    public static async Task<GeminiProvider> CreateAsync(
        ICloudCredentialService cloudConfig)
    {
        var provider = new GeminiProvider();

        string apiKey = await cloudConfig.GetSecretAsync("GEMINI_API_KEY");

        provider._geminiClient = new Client(apiKey: apiKey);

        return provider;
    }

    public async Task<string> AnalyzeTrafficIncidentAsync(
        string textDescription)
    {
        try
        {
            var response = await _geminiClient.Models.GenerateContentAsync(
                model: ModelName,
                contents: textDescription
            );

            return response.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            return $"[Gemini Error]: {ex.Message}";
        }
    }

    public async Task<string> AnalyzeImageAsync(
        byte[] imageData,
        string prompt)
    {
        try
        {
            var response = await _geminiClient.Models.GenerateContentAsync(
                model: ModelName,
                contents: new List<Content>
                {
                    new Content
                    {
                        Parts = new List<Part>
                        {
                            new Part { Text = prompt },

                            new Part
                            {
                                InlineData = new Blob
                                {
                                    MimeType = "image/jpeg",
                                    Data = imageData
                                }
                            }
                        }
                    }
                }
            );

            return response.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            return $"[Gemini Image Error]: {ex.Message}";
        }
    }
}
