// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Interfaces;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="ItemPayload"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomItemPayloadGenerator
{
    /// <summary>
    /// Random number generator
    /// </summary>
    private static readonly Random s_random = new();

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Circle"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Circle"/></returns>
    public static ItemPayload.Circle GenerateCircle()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random center
        double centerX = s_random.NextDouble() * 100;
        double centerY = s_random.NextDouble() * 100;
        Point2D center = new() { X = centerX, Y = centerY };

        // 3. Generate a random radius
        double radius = s_random.NextDouble() * 100;

        // 4. Generate a circle itempayload
        ItemPayload.Circle circle = new(style, center, radius);
        return circle;
    }
}
