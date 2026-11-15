//owner : U Sai Vignesh
// License : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

/// <summary>
/// Textbox state tests
/// </summary>
public class TextboxStateTests
{
    private readonly MockWhiteboardClient _client = new();
    private readonly MockCanvasPreview _preview = new();
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 1, 2, 3), 2.0, Color.FromArgb(255, 4, 5, 6));

    private TextboxState CreateState() => new(_client, _preview, _toolbar);

    private Textbox AddTextbox(Point2D center, string content = "")
    {
        Textbox textbox = new() { Center = center, Content = content };
        _client.Canvas.AddItem(textbox);
        return textbox;
    }

    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TextboxState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new TextboxState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new TextboxState(_client, _preview, null!));
    }

    [Fact]
    public void HandlesPointerOnTextbox_IsFalse()
    {
        Assert.False(CreateState().HandlesPointerOnTextbox);
    }

    [Fact]
    public void PointerPressed_OnEmptyArea_AddsTextboxAtPoint()
    {
        TextboxState state = CreateState();

        state.PointerPressed(new Point2D(100, 200));

        ITextbox added = Assert.IsAssignableFrom<ITextbox>(Assert.Single(_client.AddedItems));
        Assert.Equal(new Point2D(100, 200), added.Center);
        Assert.Equal(_client.UserId, added.CreatedBy);
        Assert.Equal(string.Empty, added.Content);
    }

    [Fact]
    public void PointerPressed_UsesNextZIndex()
    {
        _client.Canvas.AddItem(new Square { Center = new Point2D(500, 500), SideLegth = 10, Zindex = 4 });
        TextboxState state = CreateState();

        state.PointerPressed(new Point2D(10, 10));

        Assert.Equal(5, Assert.Single(_client.AddedItems).Zindex);
    }

    [Fact]
    public void PointerPressed_OnExistingTextbox_DoesNotAddNewTextbox()
    {
        AddTextbox(new Point2D(100, 100));
        TextboxState state = CreateState();

        state.PointerPressed(new Point2D(100, 100));

        Assert.Empty(_client.AddedItems);
    }

    [Fact]
    public void PointerPressed_CommitsPendingEditFirst()
    {
        Textbox textbox = AddTextbox(new Point2D(100, 100));
        TextboxState state = CreateState();
        state.UpdateText(textbox.Id, "hello");

        state.PointerPressed(new Point2D(400, 400));

        Assert.Equal("hello", textbox.Content);
        Assert.Single(_client.EditedItems);
        Assert.Single(_client.AddedItems);
    }

    [Fact]
    public void UpdateText_DoesNotSendUntilCommitted()
    {
        Textbox textbox = AddTextbox(new Point2D(100, 100));
        TextboxState state = CreateState();

        state.UpdateText(textbox.Id, "hello");

        Assert.True(state.HasPendingEdits);
        Assert.Empty(_client.EditedItems);
        Assert.Equal(string.Empty, textbox.Content);
    }

    [Fact]
    public void UpdateText_SameAsStoredContent_IsNotPending()
    {
        Textbox textbox = AddTextbox(new Point2D(100, 100), "same");
        TextboxState state = CreateState();

        state.UpdateText(textbox.Id, "changed");
        state.UpdateText(textbox.Id, "same");

        Assert.False(state.HasPendingEdits);
    }

    [Fact]
    public void UpdateText_NullText_TreatedAsEmpty()
    {
        Textbox textbox = AddTextbox(new Point2D(100, 100), "abc");
        TextboxState state = CreateState();

        state.UpdateText(textbox.Id, null!);
        state.CommitPendingEdits();

        Assert.Equal(string.Empty, textbox.Content);
    }

    [Fact]
    public void CommitPendingEdits_SendsEditThroughClient()
    {
        Textbox textbox = AddTextbox(new Point2D(100, 100));
        TextboxState state = CreateState();
        state.UpdateText(textbox.Id, "hello");

        state.CommitPendingEdits();

        IItem edited = Assert.Single(_client.EditedItems);
        Assert.Equal(textbox.Id, edited.Id);
        Assert.Equal("hello", ((ITextbox)_client.Canvas.Items[textbox.Id]).Content);
        Assert.False(state.HasPendingEdits);
    }

    [Fact]
    public void CommitPendingEdits_KeepsOnlyLatestText()
    {
        Textbox textbox = AddTextbox(new Point2D(100, 100));
        TextboxState state = CreateState();
        state.UpdateText(textbox.Id, "h");
        state.UpdateText(textbox.Id, "he");
        state.UpdateText(textbox.Id, "hey");

        state.CommitPendingEdits();

        Assert.Single(_client.EditedItems);
        Assert.Equal("hey", textbox.Content);
    }

    [Fact]
    public void CommitPendingEdits_CommitsEveryEditedTextbox()
    {
        Textbox first = AddTextbox(new Point2D(100, 100));
        Textbox second = AddTextbox(new Point2D(400, 400));
        TextboxState state = CreateState();
        state.UpdateText(first.Id, "one");
        state.UpdateText(second.Id, "two");

        state.CommitPendingEdits();

        Assert.Equal(2, _client.EditedItems.Count);
        Assert.Equal("one", first.Content);
        Assert.Equal("two", second.Content);
    }

    [Fact]
    public void CommitPendingEdits_IsIdempotent()
    {
        Textbox textbox = AddTextbox(new Point2D(100, 100));
        TextboxState state = CreateState();
        state.UpdateText(textbox.Id, "hello");

        state.CommitPendingEdits();
        state.CommitPendingEdits();

        Assert.Single(_client.EditedItems);
    }

    [Fact]
    public void CommitPendingEdits_ForErasedTextbox_IsDiscarded()
    {
        Textbox textbox = AddTextbox(new Point2D(100, 100));
        TextboxState state = CreateState();
        state.UpdateText(textbox.Id, "hello");
        _client.DeleteItem(textbox.Id);

        state.CommitPendingEdits();

        Assert.Empty(_client.EditedItems);
        Assert.False(state.HasPendingEdits);
    }

    [Fact]
    public void CommitPendingEdits_ForNonTextboxItem_IsIgnored()
    {
        Square square = new() { Center = new Point2D(50, 50), SideLegth = 10 };
        _client.Canvas.AddItem(square);
        TextboxState state = CreateState();
        state.UpdateText(square.Id, "hello");

        state.CommitPendingEdits();

        Assert.Empty(_client.EditedItems);
    }

    [Fact]
    public void CommitPendingEdits_WhenNothingPending_DoesNothing()
    {
        TextboxState state = CreateState();

        state.CommitPendingEdits();

        Assert.Empty(_client.EditedItems);
    }

    [Fact]
    public void PointerMoved_Released_CaptureLost_DoNothing()
    {
        TextboxState state = CreateState();

        state.PointerMoved(new Point2D(10, 10));
        state.PointerReleased(new Point2D(10, 10));
        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Empty(_client.AddedItems);
        Assert.Empty(_client.EditedItems);
    }
}
