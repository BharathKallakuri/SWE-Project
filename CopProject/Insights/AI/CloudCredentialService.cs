namespace Insights.AI;

public interface ICloudCredentialService
{
    Task<string> GetSecretAsync(string secretName);
}

public class CloudCredentialService : ICloudCredentialService
{
    public Task<string> GetSecretAsync(string secretName)
    {
        string? secret = Environment.GetEnvironmentVariable(secretName);

        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException(
                $"Secret '{secretName}' not found.");
        }

        return Task.FromResult(secret);
    }
}
