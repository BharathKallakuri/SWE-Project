// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Interfaces;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="Point2D"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomPointGenerator
{
    /// <summary>
    /// Random number generator
    /// </summary>
    private static readonly Random s_random = new();

    /// <summary>
    /// Generate a random <see cref="Point2D"/>
    /// </summary>
    /// <returns>A random <see cref="Point2D"/></returns>
    public static Point2D Generate()
    {
        double pointX = s_random.NextDouble() * 100;
        double pointY = s_random.NextDouble() * 100;
        Point2D point = new() { X = pointX, Y = pointY };
        return point;
    }
}
