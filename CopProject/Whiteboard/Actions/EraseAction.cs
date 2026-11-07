/****
    * Filename  : EraseAction.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/

using System;
using Whiteboard.Interfaces;

namespace Whiteboard.Actions;

/// <summary>
/// Erases one complete item using the shared delete action.
/// </summary>
public class EraseAction : DeleteItemsAction
{
    /// <summary>
    /// Creates an action for erasing an item.
    /// </summary>
    /// <param name="item">Item to erase.</param>
    /// <param name="userId">User erasing the item.</param>
    public EraseAction(IItem item, string userId)
        : base(
            item is null
                ? throw new ArgumentNullException(nameof(item))
                : item.Id,
            userId)
    {
    }
}
