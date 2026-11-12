/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Models;

namespace Whiteboard.Test.Mocks;

/// <summary>
/// Mock for whiteboard client
/// </summary>
/// <inheritdoc/>
public class MockWhiteboardClient : IWhiteboardClient
{
    public IWhiteboard Canvas { get; } = new WhiteboardCanvas();
    public string UserId { get; set; } = "test-user";
    public bool CanUndo { get; set; }
    public bool CanRedo { get; set; }
    public event Action HistoryChanged = delegate { };

    /// <summary>
    /// A history of items added through the client
    /// </summary>
    public List<IItem> AddedItems { get; } = [];

    /// <summary>
    /// A history of items deleted through the client
    /// </summary>
    public List<Guid> DeletedItemIds { get; } = [];

    /// <summary>
    /// A history of items edited through the client
    /// </summary>
    public List<IItem> EditedItems { get; } = [];

    public void RequestSync() { }

    public void AddItem(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        AddedItems.Add(item);
        Canvas.AddItem(item);
    }

    public void DeleteItem(Guid id)
    {
        DeletedItemIds.Add(id);
        Canvas.DeleteItem(id);
    }

    public void EditItem(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        EditedItems.Add(item);
        Canvas.EditItem(item);
    }

    public void Undo() { }
    public void Redo() { }
    public void Reset() { }
    public void OnMessageReceived(byte[] message) { }
}
