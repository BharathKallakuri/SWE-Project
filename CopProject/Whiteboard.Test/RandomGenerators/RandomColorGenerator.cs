// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="Color"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomColorGenerator : BaseGenerator
{
    /// <summary>
    /// Random colors for testing
    /// </summary>
    private static readonly List<Color> s_random_colors = [Color.Red, Color.Black, Color.Blue, Color.Yellow];

    public static Color Generate()
    {
        int randomIndex = Rng.Next(s_random_colors.Count);
        Color randomColor = s_random_colors[randomIndex];

        return randomColor;
    }
}
