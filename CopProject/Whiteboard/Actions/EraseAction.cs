using System;
using Whiteboard.Interfaces;

namespace Whiteboard.Actions;

public class EraseAction : DeleteItemsAction
{
    public EraseAction(IItem item, string userId)
        : base(
            item is null
                ? throw new ArgumentNullException(nameof(item))
                : item.Id,
            userId)
    {
    }
}
