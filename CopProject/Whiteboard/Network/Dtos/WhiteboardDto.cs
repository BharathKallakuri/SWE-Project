// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Whiteboard.Network.Dtos;

/// <summary>
/// Describes a serializable representation of the whiteboard state
/// </summary>
/// <param name="Items">List of items in the whiteboard</param>
[ExcludeFromCodeCoverage]
public record WhiteboardDto(
    List<ItemDto> Items
 );
