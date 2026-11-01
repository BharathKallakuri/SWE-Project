// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Network.Dtos;
using Whiteboard.Network.Serializations;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Serialization;

/// <summary>
/// An enum representing differen test cases for xUnit
/// </summary>
public enum XUnitShapeType
{
    Freehand,
    Line,
    Textbox,
    Circle,
    Ellipse,
    EqTriangle,
    Polygon,
    Rectangle,
    Square
}

public class ItemDtoSerializationTests
{
    /// <summary>
    /// Test that serialization and deserialization preserves the data
    /// </summary>
    /// <param name="type">One of <see cref="XUnitShapeType"/></param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="type"/> is not one of <see cref="XUnitShapeType"/></exception>
    [Theory]
    [InlineData(XUnitShapeType.Freehand)]
    [InlineData(XUnitShapeType.Line)]
    [InlineData(XUnitShapeType.Textbox)]
    [InlineData(XUnitShapeType.Circle)]
    [InlineData(XUnitShapeType.Ellipse)]
    [InlineData(XUnitShapeType.EqTriangle)]
    [InlineData(XUnitShapeType.Polygon)]
    [InlineData(XUnitShapeType.Rectangle)]
    [InlineData(XUnitShapeType.Square)]
    public void ItemDto_RoundTrip_PreservesData(XUnitShapeType type)
    {
        // Arrange
        ItemMetadata meta = RandomItemMetadataGenerator.Generate();
        ItemPayload payload = type switch {
            XUnitShapeType.Freehand => RandomItemPayloadGenerator.GenerateFreehand(),
            XUnitShapeType.Line => RandomItemPayloadGenerator.GenerateLine(),
            XUnitShapeType.Textbox => RandomItemPayloadGenerator.GenerateTextbox(),
            XUnitShapeType.Circle => RandomItemPayloadGenerator.GenerateCircle(),
            XUnitShapeType.Ellipse => RandomItemPayloadGenerator.GenerateEllipse(),
            XUnitShapeType.EqTriangle => RandomItemPayloadGenerator.GenerateEqTriangle(),
            XUnitShapeType.Polygon => RandomItemPayloadGenerator.GeneratePolygon(),
            XUnitShapeType.Rectangle => RandomItemPayloadGenerator.GenerateRectangle(),
            XUnitShapeType.Square => RandomItemPayloadGenerator.GenerateSquare(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        ItemDto original = new ItemDto(meta, payload);

        // Act
        string serialized = ItemDtoStringSerializer.Serialize(original);
        ItemDto deserialized = ItemDtoStringSerializer.Deserialize(serialized);

        // Assert
        Assert.Equivalent(original, deserialized);
    }
}
