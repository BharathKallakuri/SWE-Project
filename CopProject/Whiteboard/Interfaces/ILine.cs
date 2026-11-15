// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.UI;

namespace Whiteboard.Interfaces;

/// <summary>
/// Represents a straight line
/// </summary>
public interface ILine : IItem
{
    /// <summary>
    /// Starting endpoint of the line
    /// </summary>
    public Point2D Begin { get; set; }

    /// <summary>
    /// Ending endpoint of the line
    /// </summary>
    public Point2D End { get; set; }

    /// <summary>
    /// Color of the line
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Width of the line
    /// </summary>
    public double Width { get; set; }
}
