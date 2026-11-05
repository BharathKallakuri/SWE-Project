// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/****
    * Filename  : IWhiteboardHistory.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/


using System;
using System.Collections.Generic;
using System.Text;

namespace Whiteboard.Interfaces;

/// <summary>
/// Manages undo and redo for one local user's whiteboard actions.
/// </summary>
public interface IWhiteboardHistory
{
    /// <summary>
    /// Gets whether an action is available to undo.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Gets whether an action is available to redo.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Raised after a successful execute, undo, or redo operation.
    /// </summary>
    event Action HistoryChanged;

    /// <summary>
    /// Performs an action, records its inverse for undo,
    /// and clears the redo history after successful execution.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    void Execute(IWhiteboardAction action);

    /// <summary>
    /// Undoes the most recent action.
    /// Does nothing when no action is available to undo.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the most recently undone action.
    /// Does nothing when no action is available to redo.
    /// </summary>
    void Redo();
}
