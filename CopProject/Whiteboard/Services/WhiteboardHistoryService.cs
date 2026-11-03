// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

using Whiteboard.Interfaces;

namespace Whiteboard.Services;

public class WhiteboardHistoryService
{
    private readonly IWhiteboard _whiteboard;
    private readonly string _userId;

    private readonly Stack<IWhiteboardAction> _undo = new();
    private readonly Stack<IWhiteboardAction> _redo = new();

    private bool _isChangingHistory;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public event Action HistoryChanged = delegate { };

    public WhiteboardHistoryService(IWhiteboard whiteboard, string userId)
    {
        ArgumentNullException.ThrowIfNull(whiteboard);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        _whiteboard = whiteboard;
        _userId = userId;
    }

    public void Execute(IWhiteboardAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        ChangeHistory(() => {
            ValidateUser(action);

            action.Perform(_whiteboard);
            IWhiteboardAction inverse = GetInverse(action);

            _undo.Push(inverse);
            _redo.Clear();
        });
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        ChangeHistory(() => Transfer(_undo, _redo));
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        ChangeHistory(() => Transfer(_redo, _undo));
    }

    private void Transfer(
        Stack<IWhiteboardAction> source,
        Stack<IWhiteboardAction> destination)
    {
        // Keep the entry in its original stack until execution succeeds.
        IWhiteboardAction action = source.Peek();
        ValidateUser(action);

        action.Perform(_whiteboard);
        IWhiteboardAction inverse = GetInverse(action);

        source.Pop();
        destination.Push(inverse);
    }

    private IWhiteboardAction GetInverse(IWhiteboardAction action)
    {
        IWhiteboardAction inverse = action.CreateInverse();

#pragma warning disable IDE0270 // Use coalesce expression
        if (inverse is null)
        {
            throw new InvalidOperationException(
                "A history action must provide an inverse.");
        }
#pragma warning restore IDE0270 // Use coalesce expression

        ValidateUser(inverse);
        return inverse;
    }

    private void ValidateUser(IWhiteboardAction action)
    {
        if (!string.Equals(action.UserId, _userId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "This history belongs to a different user.");
        }
    }

    private void ChangeHistory(Action operation)
    {
        if (_isChangingHistory)
        {
            throw new InvalidOperationException(
                "Another history operation is already running.");
        }

        _isChangingHistory = true;

        try
        {
            operation();
        }
        finally
        {
            _isChangingHistory = false;
        }

        // Reached only when the operation completed successfully.
        HistoryChanged();
    }
}
