//owner : U Sai Vignesh
// License : MIT

using System;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Selection;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the select tool is chosen.
/// Clicking an item selects it, clicking empty canvas deselects.
/// </summary>
public sealed class SelectState : ViewModelStateBase
{
    /// <summary>
    /// Which items are currently selected.
    /// </summary>
    private readonly ISelectionState _selection = new SelectionState();

    /// <summary>
    /// Hit-tests the canvas and updates <see cref="_selection"/>.
    /// </summary>
    private readonly ISelectionService _selectionService;

    public SelectState(IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo)
        : base(whiteboardClient, preview, toolbarInfo)
    {
        _selectionService = new SelectionService(WhiteboardClient.Canvas, _selection);

        // Drop the selection if the selected item is erased (by anyone)
        WhiteboardClient.Canvas.ItemDeleted += OnItemDeleted;
    }

    /// <summary>
    /// Raised whenever the selected item changes or the selection is cleared.
    /// May be raised off the UI thread (e.g. when a remote user erases the item).
    /// </summary>
    public event Action? SelectionChanged;

    /// <summary>
    /// Id of the selected item, or null when nothing is selected.
    /// </summary>
    public Guid? SelectedItemId => _selection.PrimarySelectedId;

    /// <summary>
    /// The selected item from the canvas, or null when nothing is selected.
    /// </summary>
    public IItem? SelectedItem =>
        SelectedItemId is Guid id && WhiteboardClient.Canvas.Items.TryGetValue(id, out IItem? item)
            ? item
            : null;

    public override void PointerPressed(Point2D point)
    {
        Guid? previous = SelectedItemId;

        // Selects the topmost item under the pointer, or clears the selection on empty canvas
        _selectionService.SelectAtPoint(point);

        if (previous != SelectedItemId)
        {
            SelectionChanged?.Invoke();
        }
    }

    public override void PointerMoved(Point2D point)
    {
    }

    public override void PointerReleased(Point2D point)
    {
    }

    public override void PointerCaptureLost(Point2D point)
    {
    }

    /// <summary>
    /// Deselects everything. Does nothing if nothing is selected.
    /// </summary>
    public void ClearSelection()
    {
        if (SelectedItemId is null)
        {
            return;
        }

        _selection.Clear();
        SelectionChanged?.Invoke();
    }

    private void OnItemDeleted(Guid id)
    {
        if (_selection.IsSelected(id))
        {
            ClearSelection();
        }
    }
}
