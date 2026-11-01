// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
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

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Ellipse"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Ellipse"/></returns>
    public static ItemPayload.Ellipse GenerateEllipse()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random center
        double centerX = s_random.NextDouble() * 100;
        double centerY = s_random.NextDouble() * 100;
        Point2D center = new() { X = centerX, Y = centerY };

        // 3. Generate a random width and height
        double width = s_random.NextDouble() * 100;
        double height = s_random.NextDouble() * 100;

        // 4. Generate a ellipse itempayload
        ItemPayload.Ellipse ellipse = new(style, center, width, height);
        return ellipse;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.EqTriangle"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.EqTriangle"/></returns>
    public static ItemPayload.EqTriangle GenerateEqTriangle()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random center
        double centerX = s_random.NextDouble() * 100;
        double centerY = s_random.NextDouble() * 100;
        Point2D center = new() { X = centerX, Y = centerY };

        // 3. Generate a random side length
        double sideLength = s_random.NextDouble() * 100;

        // 4. Generate a eqilateral triangle itempayload
        ItemPayload.EqTriangle eqTriangle = new(style, center, sideLength);
        return eqTriangle;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Polygon"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Polygon"/></returns>
    public static ItemPayload.Polygon GeneratePolygon()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random lit of vertices
        int nVertices = s_random.Next(2, 50);
        List<Point2D> vertices = [];

        for (int i = 0; i < nVertices; i++)
        {
            // 2.a. Generate a random point
            double pointX = s_random.NextDouble() * 100;
            double pointY = s_random.NextDouble() * 100;
            Point2D point = new() { X = pointX, Y = pointY };

            vertices.Add(point);
        }

        // 3. Generate a polygon itempayload
        ItemPayload.Polygon polygon = new(style, vertices);
        return polygon;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Rectangle"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Rectangle"/></returns>
    public static ItemPayload.Rectangle GenerateRectangle()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random center
        double centerX = s_random.NextDouble() * 100;
        double centerY = s_random.NextDouble() * 100;
        Point2D center = new() { X = centerX, Y = centerY };

        // 3. Generate a random width and height
        double width = s_random.NextDouble() * 100;
        double height = s_random.NextDouble() * 100;

        // 4. Generate a rectangle itempayload
        ItemPayload.Rectangle rectangle = new(style, center, width, height);
        return rectangle;
    }
}
