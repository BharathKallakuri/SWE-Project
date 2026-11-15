// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using System.Text.Json;
using Cloud.Networking;

namespace Cloud.Test;

/// <summary>
/// Tests the message record
/// </summary>
[TestClass]
public class TestMessage
{
    /// <summary>Tests that sender ID and content are stored without modification.</summary>
    [TestMethod]
    public void SenderIdAndContentAreStoredAsGiven()
    {
        const string SenderId = "sender-123";
        const string Content = "Hello";

        Message message = new Message {
            SenderId = SenderId,
            Content = Content
        };

        // to catch if internal representation is different
        Assert.AreEqual(SenderId, message.SenderId);
        Assert.AreEqual(Content, message.Content);
    }


    /// <summary>Tests that new messages receive unique, non-empty IDs.</summary>
    [TestMethod]
    public void NewMessagesGetUniqueNonEmptyIds()
    {
        Message first = new Message { SenderId = "sender-1", Content = "Hello" };
        Message second = new Message { SenderId = "sender-1", Content = "Hello" };

        Assert.AreNotEqual(Guid.Empty, first.Id);
        Assert.AreNotEqual(first.Id, second.Id);
    }

    /// <summary>Tests that the default timestamp is set to the current UTC time.</summary>
    [TestMethod]
    public void DefaultTimestampIsCurrentUtcTime()
    {
        DateTimeOffset beforeCreation = DateTimeOffset.UtcNow;

        Message message = new Message {
            SenderId = "sender-1",
            Content = "Hello"
        };

        DateTimeOffset afterCreation = DateTimeOffset.UtcNow;

        Assert.IsTrue(message.Timestamp >= beforeCreation);
        Assert.IsTrue(message.Timestamp <= afterCreation);
        Assert.AreEqual(TimeSpan.Zero, message.Timestamp.Offset);
    }
}
