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

public class AddItemAction : IWhiteboardAction
{
    private readonly IItem[] _snapshots;

    public Guid ActionId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; }
    public DateTime ActionDateTime { get; set; } = DateTime.UtcNow;

    public AddItemAction(IItem item, string userId)
        : this(new[] { item }, userId)
    {
    }

    public AddItemAction(IEnumerable<IItem> items, string userId)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        IItem[] suppliedItems = items.ToArray();

        if (suppliedItems.Length == 0)
        {
            throw new ArgumentException(
                "An add action needs at least one item.",
                nameof(items));
        }

        if (suppliedItems.Any(item => item is null))
        {
            throw new ArgumentException(
                "An add action cannot contain a null item.",
                nameof(items));
        }

        if (suppliedItems.Select(item => item.Id).Distinct().Count()
            != suppliedItems.Length)
        {
            throw new ArgumentException(
                "An add action cannot contain duplicate item IDs.",
                nameof(items));
        }

        UserId = userId;
        _snapshots = suppliedItems.Select(ItemSnapshot.Copy).ToArray();
    }

    public void Perform(IWhiteboard whiteboard)
    {
        ArgumentNullException.ThrowIfNull(whiteboard);

        // Validate the whole group before changing the board.
        foreach (IItem snapshot in _snapshots)
        {
            if (whiteboard.Items.ContainsKey(snapshot.Id))
            {
                throw new InvalidOperationException(
                    $"Item {snapshot.Id} already exists.");
            }
        }

        // Editable board objects must be separate from saved snapshots.
        IItem[] itemsToAdd = _snapshots
            .Select(ItemSnapshot.Copy)
            .ToArray();

        foreach (IItem item in itemsToAdd)
        {
            whiteboard.AddItem(item);
        }
    }

    public IWhiteboardAction CreateInverse()
    {
        return new DeleteItemsAction(
            _snapshots.Select(item => item.Id),
            UserId);
    }
}
