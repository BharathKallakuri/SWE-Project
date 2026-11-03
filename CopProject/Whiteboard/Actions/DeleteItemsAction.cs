// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Whiteboard.Interfaces;
using Whiteboard.Services;

namespace Whiteboard.Actions;

public class DeleteItemsAction : IWhiteboardAction
{
    private readonly Guid[] _itemIds;
    private IItem[] _deletedSnapshots = Array.Empty<IItem>();

    public Guid ActionId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; }
    public DateTime ActionDateTime { get; set; } = DateTime.UtcNow;

    public DeleteItemsAction(Guid itemId, string userId)
        : this(new[] { itemId }, userId)
    {
    }

    public DeleteItemsAction(IEnumerable<Guid> itemIds, string userId)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        _itemIds = itemIds.Distinct().ToArray();

        if (_itemIds.Length == 0)
        {
            throw new ArgumentException(
                "A delete action needs at least one item ID.",
                nameof(itemIds));
        }

        UserId = userId;
    }

    public void Perform(IWhiteboard whiteboard)
    {
        ArgumentNullException.ThrowIfNull(whiteboard);

        var snapshots = new List<IItem>();

        // Find and copy every item before deleting any item.
        foreach (Guid id in _itemIds)
        {
            if (!whiteboard.Items.TryGetValue(id, out IItem item))
            {
                throw new InvalidOperationException(
                    $"Item {id} does not exist.");
            }

            snapshots.Add(ItemSnapshot.Copy(item));
        }

        foreach (Guid id in _itemIds)
        {
            if (!whiteboard.DeleteItem(id))
            {
                throw new InvalidOperationException(
                    $"Item {id} could not be deleted.");
            }
        }

        _deletedSnapshots = snapshots.ToArray();
    }

    public IWhiteboardAction CreateInverse()
    {
        if (_deletedSnapshots.Length == 0)
        {
            throw new InvalidOperationException(
                "Perform the deletion before creating its inverse.");
        }

        return new AddItemAction(_deletedSnapshots, UserId);
    }
}
