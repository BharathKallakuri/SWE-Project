/****
    * Filename  : DrawFreehandAction.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/


using System;
using System.Diagnostics.CodeAnalysis;
using Whiteboard.Interfaces;

namespace Whiteboard.Actions;

/// <summary>
/// Adds a freehand stroke using the shared add action.
/// </summary>
public class DrawFreehandAction : AddItemAction
{
    /// <summary>
    /// Creates an action for adding a freehand stroke.
    /// </summary>
    /// <param name="item">Stroke to add.</param>
    /// <param name="userId">User drawing the stroke.</param>
    public DrawFreehandAction(IFreehand item, string userId)
        : base(item, userId)
    {
    }
}



