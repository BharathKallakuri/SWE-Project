/// Author : Karthik Krishna M
/// Roll Number : 112301014
/// LICENSE : MIT

using System.Text.Json;
using Cloud.Persistence;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Whiteboard.Interfaces;
using Whiteboard.Models;
using Whiteboard.Services;
using Whiteboard.Test.Views;

namespace Whiteboard.Test.Services;

public class CloudExportIntegrationTests
{
    [Fact(Skip = "Integration test requiring live Cloud storage.")]
    public async Task ExportToCloudAsync_StoreExportData()
    {
        var exportData = new CanvasExportData {
            ImageData = [1, 2, 3, 4],
            ContentType = "image/png",
            FileName = $"test-{Guid.NewGuid():N}.png",
            PixelWidth = 10,
            PixelHeight = 20,
            ExportDateTime = DateTime.UtcNow
        };

        var canvasRender = new TempCanvasRender(exportData);
        var cloudExport = new CloudExport(canvasRender);

        string userId = $"test-user-{Guid.NewGuid():N}";
        string whiteboardId = $"test-board-{Guid.NewGuid():N}";
        string key = $"{userId}:{whiteboardId}:{exportData.FileName}";

        IStorage storage = await StorageFactory.CreateAsync(ModuleType.WhiteBoard);

        try
        {
            bool saved = await cloudExport.SaveExportDataToCloudAsync(exportData, whiteboardId, userId);

            Assert.True(saved);

            (bool Success, byte[] Data) result = await storage.Read(key);
            Assert.True(result.Success);

            using JsonDocument document = JsonDocument.Parse(result.Data);
            JsonElement root = document.RootElement;

            Assert.Equal(whiteboardId, root.GetProperty("BoardId").GetString());
            Assert.Equal(userId, root.GetProperty("OwnerId").GetString());
            Assert.Equal(exportData.FileName, root.GetProperty("FileName").GetString());
            Assert.Equal("image/png", root.GetProperty("ContentType").GetString());
            Assert.Equal("10x20", root.GetProperty("Dimensions").GetString());
            Assert.Equal(exportData.ImageData, root.GetProperty("ImageData").GetBytesFromBase64());
        }
        finally
        {
            await storage.Delete(key);
        }
    }

    private sealed class TempCanvasRender(CanvasExportData exportData) : ICanvasRender
    {
        public Task<byte[]> RenderToPngAsync(UIElement element)
        {
            return Task.FromResult(exportData.ImageData);
        }

        public Task<byte[]> RenderToJpegAsync(UIElement element)
        {
            return Task.FromResult(exportData.ImageData);
        }

        public Task<CanvasExportData> CreateExportDataAsync(UIElement element, string whiteboardId, bool isJpeg = false)
        {
            return Task.FromResult(exportData);
        }
    }

    [Fact]
    public void Constructor_ThrowsOnNullCanvasRender()
    {
        Assert.Throws<ArgumentNullException>(() => new CloudExport(null!));
    }

    [Fact]
    public async Task ExportToCloudAsync_ThrowsOnNullElement()
    {
        var exportData = new CanvasExportData { FileName = "board.png", ContentType = "image/png", ImageData = [1, 2] };
        var cloudExport = new CloudExport(new TempCanvasRender(exportData), new FakeStorage(true));

        await Assert.ThrowsAsync<ArgumentNullException>(() => cloudExport.ExportToCloudAsync(null!, "b1", "u1"));
    }

    [Fact]
    public async Task SaveExportDataToCloudAsync_ThrowsOnNullExportData()
    {
        var exportData = new CanvasExportData { FileName = "board.png", ContentType = "image/png", ImageData = [1, 2] };
        var cloudExport = new CloudExport(new TempCanvasRender(exportData), new FakeStorage(true));

        await Assert.ThrowsAsync<ArgumentNullException>(() => cloudExport.SaveExportDataToCloudAsync(null!, "b1", "u1"));
    }

    [Fact]
    public async Task SaveExportDataToCloudAsync_ReturnsTrueAndPersistsJsonRecord()
    {
        var exportData = new CanvasExportData {
            ImageData = [1, 2, 3, 4],
            ContentType = "image/png",
            FileName = "board.png",
            PixelWidth = 100,
            PixelHeight = 200,
            ExportDateTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var storage = new FakeStorage(true);
        var cloudExport = new CloudExport(new TempCanvasRender(exportData), storage);

        bool saved = await cloudExport.SaveExportDataToCloudAsync(exportData, "board-1", "user-1");

        Assert.True(saved);
        Assert.Equal("user-1:board-1:board.png", storage.LastKey);
        Assert.NotNull(storage.LastData);
        using JsonDocument doc = JsonDocument.Parse(storage.LastData!);
        Assert.Equal("board-1", doc.RootElement.GetProperty("BoardId").GetString());
        Assert.Equal("user-1", doc.RootElement.GetProperty("OwnerId").GetString());
        Assert.Equal("100x200", doc.RootElement.GetProperty("Dimensions").GetString());
    }

    [Fact]
    public async Task SaveExportDataToCloudAsync_ReturnsFalseWhenStorageFails()
    {
        var exportData = new CanvasExportData {
            ImageData = [5, 6],
            ContentType = "image/jpeg",
            FileName = "board.jpg",
            PixelWidth = 10,
            PixelHeight = 10,
            ExportDateTime = DateTime.UtcNow
        };
        var storage = new FakeStorage(false);
        var cloudExport = new CloudExport(new TempCanvasRender(exportData), storage);

        bool saved = await cloudExport.SaveExportDataToCloudAsync(exportData, "board-2", "user-2");

        Assert.False(saved);
    }

    private sealed class FakeStorage(bool createResult) : IStorage
    {
        public string? LastKey { get; private set; }
        public byte[]? LastData { get; private set; }

        public Task<bool> Create(string key, byte[] data)
        {
            LastKey = key;
            LastData = data;
            return Task.FromResult(createResult);
        }

        public Task<(bool Success, byte[] Data)> Read(string key)
        {
            return Task.FromResult((false, Array.Empty<byte>()));
        }

        public Task<(bool Success, string Etag)> ReadEtag(string key)
        {
            return Task.FromResult((false, string.Empty));
        }

        public Task<bool> Update(string key, byte[] data, string etag)
        {
            return Task.FromResult(false);
        }

        public Task<bool> Delete(string key)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CreateAll(Dictionary<string, byte[]> items)
        {
            return Task.FromResult(false);
        }

        public Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys)
        {
            return Task.FromResult((false, new Dictionary<string, byte[]>()));
        }

        public Task<(bool Success, List<string>)> GetAllKeys()
        {
            return Task.FromResult((true, new List<string>()));
        }

        public Task<(bool Success, Dictionary<string, string>)> ReadAllEtags()
        {
            return Task.FromResult((false, new Dictionary<string, string>()));
        }

        public Task<(bool Success, Dictionary<string, string> Etags)> ReadEtags(string[] keys)
        {
            return Task.FromResult((false, new Dictionary<string, string>()));
        }

        public Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags)
        {
            return Task.FromResult(false);
        }

        public Task<bool> DeleteAll(string[] keys)
        {
            return Task.FromResult(false);
        }

        public Task<(bool Success, Dictionary<string, byte[]> Items)> ReadLatest(int n)
        {
            return Task.FromResult((true, new Dictionary<string, byte[]>()));
        }
    }
}

[Collection("XamlUi")]
public class CloudExportUiTests(XamlUiThreadFixture ui)
{
    [Fact]
    public async Task ExportToCloudAsync_DelegatesToCanvasRenderAndSavesPngAndJpeg()
    {
        Canvas? canvas = null;
        await ui.RunAsync(() => canvas = new Canvas());

        var pngData = new CanvasExportData {
            ImageData = [1, 2, 3],
            ContentType = "image/png",
            FileName = "board.png",
            PixelWidth = 50,
            PixelHeight = 50,
            ExportDateTime = DateTime.UtcNow
        };
        var storage = new RecordingStorage();
        var cloudExport = new CloudExport(new StubCanvasRender(pngData), storage);

        Assert.True(await cloudExport.ExportToCloudAsync(canvas!, "b1", "u1", isJpeg: false));
        Assert.Equal("u1:b1:board.png", storage.LastKey);

        Assert.True(await cloudExport.ExportToCloudAsync(canvas!, "b1", "u1", isJpeg: true));
    }

    private sealed class StubCanvasRender(CanvasExportData data) : ICanvasRender
    {
        public Task<byte[]> RenderToPngAsync(UIElement element)
        {
            return Task.FromResult(data.ImageData);
        }

        public Task<byte[]> RenderToJpegAsync(UIElement element)
        {
            return Task.FromResult(data.ImageData);
        }

        public Task<CanvasExportData> CreateExportDataAsync(UIElement element, string whiteboardId, bool isJpeg = false)
        {
            return Task.FromResult(data);
        }
    }

    private sealed class RecordingStorage : IStorage
    {
        public string? LastKey { get; private set; }
        public Task<bool> Create(string key, byte[] data) { LastKey = key; return Task.FromResult(true); }
        public Task<(bool Success, byte[] Data)> Read(string key)
        {
            return Task.FromResult((false, Array.Empty<byte>()));
        }

        public Task<(bool Success, string Etag)> ReadEtag(string key)
        {
            return Task.FromResult((false, string.Empty));
        }

        public Task<bool> Update(string key, byte[] data, string etag)
        {
            return Task.FromResult(false);
        }

        public Task<bool> Delete(string key)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CreateAll(Dictionary<string, byte[]> items)
        {
            return Task.FromResult(false);
        }

        public Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys)
        {
            return Task.FromResult((false, new Dictionary<string, byte[]>()));
        }

        public Task<(bool Success, List<string>)> GetAllKeys()
        {
            return Task.FromResult((true, new List<string>()));
        }

        public Task<(bool Success, Dictionary<string, string>)> ReadAllEtags()
        {
            return Task.FromResult((false, new Dictionary<string, string>()));
        }

        public Task<(bool Success, Dictionary<string, string> Etags)> ReadEtags(string[] keys)
        {
            return Task.FromResult((false, new Dictionary<string, string>()));
        }

        public Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags)
        {
            return Task.FromResult(false);
        }

        public Task<bool> DeleteAll(string[] keys)
        {
            return Task.FromResult(false);
        }

        public Task<(bool Success, Dictionary<string, byte[]> Items)> ReadLatest(int n)
        {
            return Task.FromResult((true, new Dictionary<string, byte[]>()));
        }
    }
}
