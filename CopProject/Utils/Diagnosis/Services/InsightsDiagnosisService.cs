// Author : Madhav P Nair
// Roll Number : 112301020

using System;
using System.Diagnostics;
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
        Trace.TraceInformation("[Utils.Diagnosis.Services][DiagnoseAsync] Starting log diagnosis.");

        /* read the recent logs - otherwise may hit token limit */
        string log = await FileTraceListener.ReadRecentLogAsync();

        Trace.TraceInformation(
            $"[Utils.Diagnosis.Services][DiagnoseAsync] Retrieved log data. Length: {log.Length} characters.");

        if (string.IsNullOrWhiteSpace(log))
        {
            Trace.TraceWarning("[Utils.Diagnosis.Services][DiagnoseAsync] No log entries available for diagnosis.");
            return "No log entries are available for diagnosis";
        }

        try
        {
            /* integrate with Insights to get a diagnosis */
            Trace.TraceInformation("[Utils.Diagnosis.Services][DiagnoseAsync] Creating Gemini provider.");
            GeminiProvider provider = await GeminiProvider.CreateAsync(new CloudCredentialService());

            Trace.TraceInformation("[Utils.Diagnosis.Services][DiagnoseAsync] Gemini provider created successfully.");
            InsightsManager insights = new(provider);

            Trace.TraceInformation("[Utils.Diagnosis.Services][DiagnoseAsync] Sending logs to Insights for diagnosis.");
            string diagnosis = await insights.DiagnoseLogAsync(log); /*new insights api */

            Trace.TraceInformation("[Utils.Diagnosis.Services][DiagnoseAsync] Log diagnosis completed successfully.");
            return diagnosis;
        }
        catch (Exception ex)
        {
            Trace.TraceError(
                $"[Utils.Diagnosis.Services][DiagnoseAsync] Diagnosis failed: {ex}");

            throw;
        }
    }
}
