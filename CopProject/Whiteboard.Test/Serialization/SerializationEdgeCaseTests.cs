/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using System.Diagnostics;
using Whiteboard.Interfaces;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Mappers;
using Whiteboard.Network.Serializations;

namespace Whiteboard.Test.Serialization;

public class SerializationEdgeCaseTests
{
    [Fact]
    public void ItemMapper_ToDto_ThrowsUnreachableExceptionForUnknownItem()
    {
        Assert.Throws<UnreachableException>(() => ItemMapper.ToDto(new UnknownItem()));
    }

    [Fact]
    public void ItemMapper_FromDto_ThrowsNotImplementedExceptionForUnknownPayload()
    {
        ItemMetadata metadata = new(Guid.NewGuid(), 0, "alice", DateTime.UtcNow, DateTime.UtcNow, 0);
        ItemDto dto = new(metadata, null!);

        Assert.Throws<NotImplementedException>(() => ItemMapper.FromDto(dto));
    }

    [Fact]
    public void ByteSerializers_Deserialize_ReturnsDefaultForNullAndEmptyArrays()
    {
        ItemDtoByteSerializer itemSerializer = new();
        WhiteboardDtoByteSerializer boardSerializer = new();
        ClientMessageByteSerializer clientSerializer = new();
        ServerMessageByteSerializer serverSerializer = new();

        Assert.Null(itemSerializer.Deserialize(null!));
        Assert.Null(itemSerializer.Deserialize([]));
        Assert.Null(boardSerializer.Deserialize(null!));
        Assert.Null(boardSerializer.Deserialize([]));
        Assert.Null(clientSerializer.Deserialize(null!));
        Assert.Null(clientSerializer.Deserialize([]));
        Assert.Null(serverSerializer.Deserialize(null!));
        Assert.Null(serverSerializer.Deserialize([]));
    }

    private sealed class UnknownItem : Whiteboard.Items.Item
    {
        public override Rect2D BoundingBox => new(0, 0, 0, 0);
        public override bool HitTest(Point2D point) => false;
        public override void MoveBy(double dx, double dy) { }
    }
}
