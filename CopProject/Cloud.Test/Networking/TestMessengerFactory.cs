// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using Cloud.Networking;
using Cloud.Networking.Decorators;

namespace Cloud.Test;

/// <summary>
/// Tests the MessengerFactory class
/// </summary>
[TestClass]
public class TestMessengerFactory
{
    private const string Url = "http://localhost/hub";

    /// <summary>Tests that GetMessenger returns a diagnostic messenger</summary>
    [TestMethod]
    public async Task GetMessengerReturnsADiagnosticMessenger()
    {
        await using IMessenger messenger = MessengerFactory.GetMessenger(Url);

        Assert.IsInstanceOfType<DiagnosticMessenger>(messenger);
    }

    /// <summary>Tests that GetSilentMessenger returns a silent messenger</summary>
    [TestMethod]
    public async Task GetSilentMessengerReturnsASilentMessenger()
    {
        await using IMessenger messenger = MessengerFactory.GetSilentMessenger(Url);

        Assert.IsInstanceOfType<SilentMessenger>(messenger);
    }
}
