// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;

namespace Cloud.Test;

[TestClass]
public class TestContract
{
    [TestMethod]
    public void CheckIfContractMethodNamesAreCorrect()
    {
        // it will be caught if someone tries to change the hardcoded values
        Assert.AreEqual("SendMessage", Contract.SendMessage);
        Assert.AreEqual("ReceiveMessage", Contract.ReceiveMessage);
    }
}
