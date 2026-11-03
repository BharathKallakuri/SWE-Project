using System;

using Whiteboard.Interfaces;

namespace Whiteboard.Actions;

public class DrawFreehandAction : AddItemAction
{
    public DrawFreehandAction(IFreehand item, string userId)
        : base(item, userId)
    {
    }
}



