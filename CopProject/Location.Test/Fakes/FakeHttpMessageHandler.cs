using System.Net;
using System.Text;

namespace Location.Test.Fakes;

/// <summary>
/// Returns a fixed response so the geocoding service never hits the real network.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseJson;
    private readonly HttpStatusCode _statusCode;

    /// <summary>
    /// Initializes a new instance of the class "FakeHttpMessageHandler".
    /// </summary>
    public FakeHttpMessageHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseJson = responseJson;
        _statusCode = statusCode;
    }

    /// <summary>
    /// How many requests were sent.
    /// </summary>
    public int RequestCount { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        HttpResponseMessage response = new(_statusCode) {
            Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
