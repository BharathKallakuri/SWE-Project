// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Network.Dtos;
using Whiteboard.Network.Serializations;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Serialization;

public class ItemDtoLeftInverseTest
{
    /// <summary>
    /// Assert that serialization then deserialization of <see cref="ItemPayload.Line"/> Dto returns the same item
    /// </summary>
    [Fact]
    public void LineItemDto()
    {
        // Arrange
        // 1. Generate a random ItemMetadata
        ItemMetadata randomItemMetadata = RandomItemMetadataGenerator.Generate();
        // 2. Generate a random line payload
        ItemPayload.Line randomLine = RandomItemPayloadGenerator.GenerateLine();
        // 3. Generate random line item dto
        ItemDto randomItem = new(randomItemMetadata, randomLine);

        // Act
        // 1. Serialize
        string serialized = ItemDtoStringSerializer.Serialize(randomItem);
        // 2. Deserialize
        ItemDto deserialized = ItemDtoStringSerializer.Deserialize(serialized);

        // Assert
        Assert.Equal(randomItem, deserialized);
    }
}
