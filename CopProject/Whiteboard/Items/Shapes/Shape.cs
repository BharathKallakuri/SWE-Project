// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI;
using Whiteboard.Interfaces.Shapes;
using Windows.UI;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public abstract class Shape : Item, IShape
{
    public Color FillColor { get; set; } = Color.FromArgb(255, 255, 255, 255);  // White
    public Color BorderColor { get; set; } = Color.FromArgb(255, 0, 0, 0);      // Black
    public double BorderWidth { get; set; } = 10;
}
