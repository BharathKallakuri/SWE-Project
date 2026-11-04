// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Cloud.Networking;

namespace Cloud.Test;

[TestClass]
public class TestMessage
{
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

    [TestMethod]
    public void NewMessagesGetUniqueNonEmptyIds()
    {
        Message first = new Message { SenderId = "sender-1", Content = "Hello" };
        Message second = new Message { SenderId = "sender-1", Content = "Hello" };

        Assert.AreNotEqual(Guid.Empty, first.Id);
        Assert.AreNotEqual(first.Id, second.Id);
    }

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

    [TestMethod]
    public void IdIsPartOfEquality()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Message first = new Message {
            SenderId = "sender-1",
            Content = "Hello",
            Timestamp = now
        };

        Message second = new Message {
            SenderId = "sender-1",
            Content = "Hello",
            Timestamp = now
        };

        // Everything matches except the message id.
        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void JsonSerializationTest()
    {
        Message original = new Message {
            SenderId = "sender-1",
            Content = "Hello"
        };

        string json = JsonSerializer.Serialize(original);
        Message? deserialized = JsonSerializer.Deserialize<Message>(json);

        Assert.AreEqual(original, deserialized);
    }

    [TestMethod]
    public void DeserializationFailingTest()
    {
        const string Json = """{ "id": "00000000-0000-0000-0000-000000000001" }""";

        Assert.ThrowsExactly<JsonException>(
            () => JsonSerializer.Deserialize<Message>(Json));
    }
}
