//owner : U Sai Vignesh
//roll number : 112301034
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

    private TextboxState CreateState()
    {
        return new(_client, _preview, _toolbar);
    }

    // puts a textbox straight on the canvas, so it doesn't show up in AddedItems
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

    // textbox tool has to let clicks through to the textbox, otherwise you can't type in it
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

    // clicking an existing textbox is for editing it, not stacking another one on top
    [Fact]
    public void PointerPressed_OnExistingTextbox_DoesNotAddNewTextbox()
    {
        AddTextbox(new Point2D(100, 100));
        TextboxState state = CreateState();

        state.PointerPressed(new Point2D(100, 100));

        Assert.Empty(_client.AddedItems);
    }

    // if you type something and click somewhere else, the text shouldn't be lost
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

    // typing and then undoing back to the original text means nothing actually changed
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

    // someone can erase the textbox while we're still typing in it
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

    // content got updated to the same text before we committed (eg. from another client),
    // so there's nothing to send
    [Fact]
    public void CommitPendingEdits_ContentAlreadyMatches_DoesNotSendEdit()
    {
        Textbox textbox = AddTextbox(new Point2D(100, 100));
        TextboxState state = CreateState();
        state.UpdateText(textbox.Id, "hello");
        textbox.Content = "hello";

        state.CommitPendingEdits();

        Assert.Empty(_client.EditedItems);
    }

    // these are empty for the textbox tool, just making sure they don't do anything
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
