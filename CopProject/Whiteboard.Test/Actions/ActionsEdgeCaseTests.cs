/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Actions;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;

namespace Whiteboard.Test.Actions;

public class ActionsEdgeCaseTests
{
    [Fact]
    public void AddItemAction_ThrowsWhenCollectionContainsNullOrDuplicateIds()
    {
        Circle circle = new() { Center = new Point2D(10, 10), Radius = 5 };

        Assert.Throws<ArgumentException>(() => new AddItemAction(new IItem[] { null! }, "alice"));
        Assert.Throws<ArgumentException>(() => new AddItemAction(new IItem[] { circle, circle }, "alice"));
    }

    [Fact]
    public void DeleteItemsAction_ThrowsWhenItemIdsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new DeleteItemsAction(Array.Empty<Guid>(), "alice"));
    }

    [Fact]
    public void DeleteItemsAction_PerformThrowsWhenDeleteReturnsFalse()
    {
        Circle circle = new() { Center = new Point2D(10, 10), Radius = 5 };
        FailingDeleteWhiteboard board = new(circle);
        DeleteItemsAction action = new(circle.Id, "alice");

        Assert.Throws<InvalidOperationException>(() => action.Perform(board));
    }

    [Fact]
    public void EraseAction_ThrowsWhenItemIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new EraseAction(null!, "alice"));
    }

    private sealed class FailingDeleteWhiteboard : IWhiteboard
    {
        private readonly Dictionary<Guid, IItem> _items = new();

        public FailingDeleteWhiteboard(IItem item) => _items[item.Id] = item;

        public IDictionary<Guid, IItem> Items { get => _items; set { } }
        public event Action<IItem>? ItemAdded { add { } remove { } }
        public event Action<Guid>? ItemDeleted { add { } remove { } }
        public event Action<IItem>? ItemUpdated { add { } remove { } }

        public void AddItem(IItem item) { }
        public bool DeleteItem(Guid id) => false;
        public bool EditItem(IItem item) => false;
        public IItem? HitTestTopmost(Point2D point) => null;
        public List<IItem> HitTestAll(Point2D point) => [];
        public List<IItem> InRect(Rect2D rect) => [];
    }
}
