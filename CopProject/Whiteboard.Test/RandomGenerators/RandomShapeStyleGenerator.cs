// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="ShapeStyle"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomShapeStyleGenerator
{
    /// <summary>
    /// Random number generator
    /// </summary>
    private static readonly Random s_random = new();

    /// <summary>
    /// Random colors for testing
    /// </summary>
    private static readonly List<Color> s_random_colors = [Color.Red, Color.Black, Color.Blue, Color.Yellow];

    /// <summary>
    /// Generate a random <see cref="ShapeStyle"/>
    /// </summary>
    /// <returns>A random <see cref="ShapeStyle"/></returns>
    public static ShapeStyle Generate()
    {
        // 1. Generate a random fill color
        int randomIndex = s_random.Next(s_random_colors.Count);
        Color randomColor = s_random_colors[randomIndex];
        string fillColorString = randomColor.ToString();

        // 2. Generate a random border color
        randomIndex = s_random.Next(s_random_colors.Count);
        randomColor = s_random_colors[randomIndex];
        string borderColorString = randomColor.ToString();

        // 3. Generate a random border width
        double borderWidth = s_random.NextDouble() * 10;

        // 4. Generate shape style
        ShapeStyle style = new(fillColorString, borderColorString, borderWidth);
        return style;
    }
}
