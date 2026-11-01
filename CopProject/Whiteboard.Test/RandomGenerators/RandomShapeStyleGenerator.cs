// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="ShapeStyle"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomShapeStyleGenerator : BaseGenerator
{
    /// <summary>
    /// Generate a random <see cref="ShapeStyle"/>
    /// </summary>
    /// <returns>A random <see cref="ShapeStyle"/></returns>
    public static ShapeStyle Generate()
    {
        // 1. Generate a random fill color
        string fillColorString = RandomColorGenerator.Generate().ToString();

        // 2. Generate a random border color
        string borderColorString = RandomColorGenerator.Generate().ToString();

        // 3. Generate a random border width
        double borderWidth = Rng.NextDouble() * 10;

        // 4. Generate shape style
        ShapeStyle style = new(fillColorString, borderColorString, borderWidth);
        return style;
    }
}
