// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Interfaces;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random list of <see cref="Point2D"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomPointListGenerator
{
    /// <summary>
    /// Random number generator
    /// </summary>
    private static readonly Random s_random = new();

    public static List<Point2D> Generate()
    {
        int nPoints = s_random.Next(2, 50);
        List<Point2D> points = [];

        for (int i = 0; i < nPoints; i++)
        {
            Point2D point = RandomPointGenerator.Generate();

            points.Add(point);
        }

        return points;
    }
}
