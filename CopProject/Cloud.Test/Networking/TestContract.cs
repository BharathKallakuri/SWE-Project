// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using Cloud.Networking;

namespace Cloud.Test;

/// <summary>
/// Tests the Contract class
/// </summary>
[TestClass]
public class TestContract
{
    /// <summary>Tests that method names match their expected hardcoded values.</summary>
    [TestMethod]
    public void CheckIfContractMethodNamesAreCorrect()
    {
        // Ensures the contract method names remain unchanged.
        Assert.AreEqual("SendMessage", Contract.SendMessage);
        Assert.AreEqual("ReceiveMessage", Contract.ReceiveMessage);
        Assert.AreEqual("GetMessagesSince", Contract.GetMessagesSince);
    }
}
