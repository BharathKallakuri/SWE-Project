/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;
using Whiteboard.Services;

namespace Whiteboard.Test.Services;

public class HistoryServiceEdgeCaseTests
{
    [Fact]
    public void SharedHistory_IgnoresDuplicateAddMissingDeleteAndMissingEdit()
    {
        WhiteboardCanvas canvas = new();
        SharedWhiteboardHistoryService history = new(canvas);
        Circle circle = new() { Center = new Point2D(10, 10), Radius = 5 };

        history.AddItem("alice", circle);
        history.AddItem("alice", circle); // duplicate add ignored
        Assert.Single(canvas.Items);

        history.DeleteItem("alice", Guid.NewGuid()); // missing delete ignored
        Assert.Single(canvas.Items);

        Circle unknown = new() { Center = new Point2D(20, 20), Radius = 5 };
        history.EditItem("alice", unknown); // missing edit ignored
        Assert.Single(canvas.Items);
    }

    [Fact]
    public void SharedHistory_SkipsNoOpEditAndEmptyUndoRedo()
    {
        WhiteboardCanvas canvas = new();
        SharedWhiteboardHistoryService history = new(canvas);
        Circle circle = new() { Center = new Point2D(10, 10), Radius = 5 };

        // Empty undo / redo skipped
        history.Undo("alice");
        history.Redo("alice");
        Assert.Empty(canvas.Items);

        history.AddItem("alice", circle);
        // Identical edit skipped (does not add another undo entry)
        history.EditItem("alice", canvas.Items[circle.Id]);
        history.Undo("alice");
        Assert.Empty(canvas.Items);
    }

    [Fact]
    public void SharedHistory_ApplyRethrowsWhenCanvasListenerThrows()
    {
        WhiteboardCanvas canvas = new();
        SharedWhiteboardHistoryService history = new(canvas);
        canvas.ItemAdded += _ => throw new InvalidOperationException("listener failure");

        Circle circle = new() { Center = new Point2D(10, 10), Radius = 5 };
        Assert.Throws<InvalidOperationException>(() => history.AddItem("alice", circle));
    }

    [Fact]
    public void WhiteboardHistoryService_ThrowsWhenInverseIsNull()
    {
        WhiteboardCanvas canvas = new();
        WhiteboardHistoryService history = new(canvas, "alice");

        Assert.Throws<InvalidOperationException>(() => history.Execute(new NullInverseAction("alice")));
    }

    [Fact]
    public void WhiteboardHistoryService_ThrowsOnReentrantHistoryOperation()
    {
        WhiteboardCanvas canvas = new();
        WhiteboardHistoryService history = new(canvas, "alice");

        Assert.Throws<InvalidOperationException>(() => history.Execute(new ReentrantAction("alice", history)));
    }

    private sealed class NullInverseAction(string userId) : IWhiteboardAction
    {
        public Guid ActionId { get; set; } = Guid.NewGuid();
        public DateTime ActionDateTime { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = userId;
        public void Perform(IWhiteboard canvas) { }
        public IWhiteboardAction CreateInverse()
        {
            return null!;
        }
    }

    private sealed class ReentrantAction(string userId, WhiteboardHistoryService history) : IWhiteboardAction
    {
        public Guid ActionId { get; set; } = Guid.NewGuid();
        public DateTime ActionDateTime { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = userId;
        public void Perform(IWhiteboard canvas)
        {
            history.Execute(this);
        }

        public IWhiteboardAction CreateInverse()
        {
            return this;
        }
    }
}
