// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents a rectangle shape
/// </summary>
public interface IRectangle : IShape
{
    /// <summary>
    /// Center of the rectangle
    /// </summary>
    public Point2D Center { get; init; }

    /// <summary>
    /// Width of the rectangle
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Height of the rectangle
    /// </summary>
    public double Height { get; set; }
}
