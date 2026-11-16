/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using System.Text.Json;
using Whiteboard.Interfaces;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Messages;

namespace Whiteboard.Test.Network;

public class MessageAndDtoTests
{
    private static ItemDto CreateDummyItemDto(Guid id)
    {
        ItemMetadata meta = new(id, 1.0, "user", DateTime.UtcNow, DateTime.UtcNow, 0.0);
        ItemPayload payload = new ItemPayload.Line(new Point2D(0, 0), new Point2D(10, 10), "#000000", 2.0);
        return new ItemDto(meta, payload);
    }

    [Fact]
    public void ReqRestoreSnapshot_Properties_And_Serialization()
    {
        WhiteboardDto dto = new([CreateDummyItemDto(Guid.NewGuid())]);
        ClientMessage.ReqRestoreSnapshot msg = new(dto) { UserId = "user-123" };

        Assert.Equal("user-123", msg.UserId);
        Assert.Equal(dto, msg.Whiteboard);

        string json = JsonSerializer.Serialize<ClientMessage>(msg);
        Assert.Contains("req_restore_snapshot", json);

        ClientMessage? deserialized = JsonSerializer.Deserialize<ClientMessage>(json);
        Assert.NotNull(deserialized);
        ClientMessage.ReqRestoreSnapshot restoreMsg = Assert.IsType<ClientMessage.ReqRestoreSnapshot>(deserialized);
        Assert.Equal("user-123", restoreMsg.UserId);
        Assert.Single(restoreMsg.Whiteboard.Items);
    }

    [Fact]
    public void ClientMessages_DerivedRecords_Properties_And_Equality()
    {
        Guid id = Guid.NewGuid();
        ItemDto item = CreateDummyItemDto(id);

        var reqUpdate = new ClientMessage.ReqUpdate { UserId = "u1" };
        var reqAdd = new ClientMessage.ReqAddItem(id, item) { UserId = "u1" };
        var reqDelete = new ClientMessage.ReqDeleteItem(id) { UserId = "u1" };
        var reqUpdateItem = new ClientMessage.ReqUpdateItem(id, item) { UserId = "u1" };
        var reqUndo = new ClientMessage.ReqUndo { UserId = "u1" };
        var reqRedo = new ClientMessage.ReqRedo { UserId = "u1" };

        Assert.Equal("u1", reqUpdate.UserId);
        Assert.Equal(id, reqAdd.Id);
        Assert.Equal(item, reqAdd.Item);
        Assert.Equal(id, reqDelete.Id);
        Assert.Equal(id, reqUpdateItem.Id);
        Assert.Equal(item, reqUpdateItem.Item);
        Assert.Equal("u1", reqUndo.UserId);
        Assert.Equal("u1", reqRedo.UserId);

        ClientMessage.ReqUpdate reqUpdateClone = reqUpdate with { UserId = "u2" };
        Assert.Equal("u2", reqUpdateClone.UserId);
        Assert.NotEqual(reqUpdate, reqUpdateClone);
    }

    [Fact]
    public void ServerMessages_DerivedRecords_Properties_And_Equality()
    {
        Guid id = Guid.NewGuid();
        ItemDto item = CreateDummyItemDto(id);
        WhiteboardDto wb = new([item]);
        List<HistoryEntryDto> history = [new HistoryEntryDto(id, null, item)];

        var resUpdate = new ServerMessage.ResUpdate(wb);
        var resAdd = new ServerMessage.ResAddItem(id, item);
        var resDelete = new ServerMessage.ResDeleteItem(id);
        var resUpdateItem = new ServerMessage.ResUpdateItem(id, item);
        var resHistory = new ServerMessage.ResHistory("u1", history, []);

        Assert.Equal(wb, resUpdate.Whiteboard);
        Assert.Equal(id, resAdd.Id);
        Assert.Equal(item, resAdd.Item);
        Assert.Equal(id, resDelete.Id);
        Assert.Equal(id, resUpdateItem.Id);
        Assert.Equal(item, resUpdateItem.Item);
        Assert.Equal("u1", resHistory.UserId);
        Assert.Single(resHistory.Undo);
        Assert.Empty(resHistory.Redo);

        ServerMessage.ResAddItem resAddClone = resAdd with { };
        Assert.Equal(resAdd, resAddClone);
    }

    [Fact]
    public void ItemDto_Records_Properties_And_Equality()
    {
        Guid id = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        ItemMetadata meta = new(id, 1.0, "u1", now, now, 0.0);
        ShapeStyle style = new("#FFFFFF", "#000000", 2.0);

        Assert.Equal(id, meta.Id);
        Assert.Equal("u1", meta.CreatedBy);
        Assert.Equal(now, meta.CreatedDateTime);

        var linePayload = new ItemPayload.Line(new Point2D(0, 0), new Point2D(10, 10), "#000000", 2.0);
        var freehandPayload = new ItemPayload.Freehand([new Point2D(0, 0), new Point2D(5, 5)], "#FF0000", 3.0);
        var textPayload = new ItemPayload.Textbox(new Point2D(10, 10), "text", "Arial", 14, "#000000");
        var circlePayload = new ItemPayload.Circle(style, new Point2D(10, 10), 15);
        var ellipsePayload = new ItemPayload.Ellipse(style, new Point2D(10, 10), 30, 20);
        var trianglePayload = new ItemPayload.EqTriangle(style, new Point2D(10, 10), 25);
        var rectPayload = new ItemPayload.Rectangle(style, new Point2D(10, 10), 40, 30);
        var squarePayload = new ItemPayload.Square(style, new Point2D(10, 10), 25);
        var polyPayload = new ItemPayload.Polygon(style, [new Point2D(0, 0), new Point2D(10, 0), new Point2D(5, 10)]);

        Assert.Equal(15, circlePayload.Radius);
        Assert.Equal(30, ellipsePayload.Width);
        Assert.Equal(25, trianglePayload.SideLength);
        Assert.Equal(40, rectPayload.Width);
        Assert.Equal(25, squarePayload.SideLength);
        Assert.Equal(3, polyPayload.Vertices.Count);
        Assert.Equal("text", textPayload.Content);
        Assert.Equal(2, freehandPayload.Points.Count);
        Assert.Equal(new Point2D(10, 10), linePayload.End);

        ItemDto fullDto = new(meta, rectPayload);

        Assert.Equal(meta, fullDto.Metadata);
        Assert.Equal(rectPayload, fullDto.Payload);
    }
}
