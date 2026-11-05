// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/****
    * Filename  : HistoryTests.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/



using System;
using System.Collections.Generic;
using Whiteboard.Actions;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Models;
using Whiteboard.Services;
using Xunit;

using RectangleItem = Whiteboard.Items.Shapes.Rectangle;

namespace Whiteboard.Test.History;

/// <summary>
/// Tests local undo, redo, and saved item state.
/// </summary>
public class HistoryTests
{
    private const string User = "sangam";

    /// <summary>
    /// Checks that undo and redo preserve the added item's identity and data.
    /// </summary>
    [Fact]
    public void AddUndoRedoPreservesItemId()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);
        var item = new RectangleItem { Width = 100 };

        history.Execute(new AddItemAction(item, User));

        Assert.True(board.Items.ContainsKey(item.Id));
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        history.Undo();

        Assert.Empty(board.Items);
        Assert.True(history.CanRedo);

        history.Redo();

        var restored = Assert.IsType<RectangleItem>(board.Items[item.Id]);
        Assert.Equal(100d, restored.Width);
    }

    /// <summary>
    /// Checks that a group deletion is undone and redone as one action.
    /// </summary>
    [Fact]
    public void GroupDeletionIsOneUndoStep()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        var first = new RectangleItem();
        var second = new RectangleItem();

        history.Execute(new AddItemAction(first, User));
        history.Execute(new AddItemAction(second, User));

        history.Execute(new DeleteItemsAction(
            new[] { first.Id, second.Id }, User));

        Assert.Empty(board.Items);

        history.Undo();

        Assert.Equal(2, board.Items.Count);

        history.Redo();

        Assert.Empty(board.Items);
    }

    /// <summary>
    /// Checks that a successful new action clears redo.
    /// </summary>
    [Fact]
    public void SuccessfulNewActionClearsRedo()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        history.Execute(new AddItemAction(new RectangleItem(), User));
        history.Undo();

        Assert.True(history.CanRedo);

        history.Execute(new AddItemAction(new RectangleItem(), User));

        Assert.False(history.CanRedo);
    }

    /// <summary>
    /// Checks that a failed group deletion preserves the board and redo.
    /// </summary>
    [Fact]
    public void MissingItemPreventsWholeGroupDeletionAndPreservesRedo()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);
        var first = new RectangleItem();
        var second = new RectangleItem();

        history.Execute(new AddItemAction(first, User));
        history.Execute(new AddItemAction(second, User));

        // Keep one item on the board and one action available for redo.
        history.Undo();

        Assert.Throws<InvalidOperationException>(() =>
            history.Execute(new DeleteItemsAction(
                new[] { first.Id, Guid.NewGuid() }, User)));

        Assert.True(board.Items.ContainsKey(first.Id));
        Assert.True(history.CanRedo);

        history.Redo();

        Assert.Equal(2, board.Items.Count);
    }

    /// <summary>
    /// Checks that a duplicate add does not overwrite data or enter history.
    /// </summary>
    [Fact]
    public void DuplicateAddDoesNotOverwriteOrCreateHistoryEntry()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        var first = new RectangleItem { Width = 100 };
        history.Execute(new AddItemAction(first, User));

        var duplicate = new RectangleItem {
            Id = first.Id,
            Width = 900
        };

        Assert.Throws<InvalidOperationException>(() =>
            history.Execute(new AddItemAction(duplicate, User)));

        var current = Assert.IsType<RectangleItem>(board.Items[first.Id]);
        Assert.Equal(100d, current.Width);

        // Only the original successful add should need undoing.
        history.Undo();

        Assert.Empty(board.Items);
        Assert.False(history.CanUndo);
    }

    /// <summary>
    /// Checks that changing a removed stroke does not change its saved copy.
    /// </summary>
    [Fact]
    public void DeletedStrokeSnapshotIsIndependentOfRemovedObject()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        var stroke = new FreehandItem {
            Points = new List<Point2D>
            {
                new Point2D(10, 20),
                new Point2D(30, 40)
            },
            StrokeWidth = 3
        };

        history.Execute(new AddItemAction(stroke, User));

        var boardStroke =
            Assert.IsType<FreehandItem>(board.Items[stroke.Id]);

        history.Execute(new DeleteItemsAction(stroke.Id, User));

        // The removed object can still be changed through an old reference.
        boardStroke.Points[0] = new Point2D(999, 999);
        boardStroke.Points.Add(new Point2D(500, 500));

        history.Undo();

        var restored =
            Assert.IsType<FreehandItem>(board.Items[stroke.Id]);

        Assert.Equal(2, restored.Points.Count);
        Assert.Equal(new Point2D(10, 20), restored.Points[0]);
        Assert.NotSame(boardStroke.Points, restored.Points);
    }

    /// <summary>
    /// Checks that another user's action is rejected.
    /// </summary>
    [Fact]
    public void AnotherUsersActionIsRejected()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        Assert.Throws<InvalidOperationException>(() =>
            history.Execute(
                new AddItemAction(new RectangleItem(), "another-user")));

        Assert.Empty(board.Items);
        Assert.False(history.CanUndo);
    }

    /// <summary>
    /// Checks that undo and redo do nothing when history is empty.
    /// </summary>
    [Fact]
    public void EmptyUndoAndRedoDoNothing()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        history.Undo();
        history.Redo();

        Assert.Empty(board.Items);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }
}
