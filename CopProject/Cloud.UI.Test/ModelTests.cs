// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using Cloud.UI.Models;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Test;

/// <summary>
/// Tests the display texts of a notification item.
/// </summary>
[TestClass]
public class NotificationItemTests
{
    private static NotificationItem Create(string sender = "alice", DateTimeOffset? at = null)
    {
        return new() {
            Id = Guid.NewGuid(),
            SenderId = sender,
            Content = "hi",
            ReceivedAt = at ?? new DateTimeOffset(2026, 3, 4, 15, 30, 0, TimeSpan.Zero)
        };
    }

    /// <summary>TimeText is the short local time.</summary>
    [TestMethod]
    public void TimeTextIsShortLocalTime()
    {
        NotificationItem item = Create();

        Assert.AreEqual(item.ReceivedAt.ToLocalTime().ToString("t"), item.TimeText);
    }

    /// <summary>Subtitle is the sender, a dot and the time.</summary>
    [TestMethod]
    public void SubtitleIsSenderDotTime()
    {
        NotificationItem item = Create("bob");

        Assert.AreEqual("bob · " + item.TimeText, item.Subtitle);
    }

}

/// <summary>
/// Tests property-change notification of the observable base class.
/// </summary>
[TestClass]
public class ObservableObjectTests
{
    private sealed class Sample : ObservableObject
    {
        private int _value;

        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }

    /// <summary>A new value is stored and raises PropertyChanged with the sender and property name.</summary>
    [TestMethod]
    public void SetPropertyNewValueRaisesWithPropertyName()
    {
        Sample sample = new();
        List<string?> names = new();
        object? sender = null;
        sample.PropertyChanged += (s, e) => { sender = s; names.Add(e.PropertyName); };

        sample.Value = 5;

        CollectionAssert.AreEqual(new[] { nameof(Sample.Value) }, names);
        Assert.AreSame(sample, sender);
        Assert.AreEqual(5, sample.Value);
    }

    /// <summary>Setting the same value raises nothing.</summary>
    [TestMethod]
    public void SetPropertySameValueDoesNotRaise()
    {
        Sample sample = new() { Value = 5 };
        int raised = 0;
        sample.PropertyChanged += (_, _) => raised++;

        sample.Value = 5;

        Assert.AreEqual(0, raised);
    }

}
