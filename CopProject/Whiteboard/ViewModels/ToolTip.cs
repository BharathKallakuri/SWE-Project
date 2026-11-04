// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Whiteboard.ViewModels;

/// <summary>
/// Information to show when the user hovers over a whiteboard item
/// </summary>
/// <param name="CreatedBy">Name of the user who created the object</param>
/// <param name="CreatedDateTime">Time of creation</param>
public record ToolTip(
    string CreatedBy,
    DateTime CreatedDateTime
);
