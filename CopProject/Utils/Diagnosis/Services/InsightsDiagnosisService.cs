using System;
using System.Threading.Tasks;
using Insights.AI;
using Utils.Diagnostics;

namespace Utils.Diagnosis.Services;

///// <summary>Reads this app run's log and asks Insights for a diagnosis.</summary>
public sealed class InsightsDiagnosisService
{
    /// <summary>
    /// Reads the log and asks Insights for a diagnosis.
    /// </summary>
    /// <returns></returns>
    public async Task<string> DiagnoseAsync()
    {
        /* read the recent logs - otherwise may hit token limit */
        string log = await FileTraceListener.ReadRecentLogAsync();

        if (string.IsNullOrWhiteSpace(log))
        {
            return "No log entries are available for diagnosis yet.";
        }

        /* integrate with Insights to get a diagnosis */
        GeminiProvider provider = await GeminiProvider.CreateAsync(new CloudCredentialService());
        InsightsManager insights = new(provider);

        return await insights.DiagnoseLogAsync(log); /*new insights api */
    }
}
