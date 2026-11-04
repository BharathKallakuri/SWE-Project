using System;
using System.Threading.Tasks;
using Cloud.Persistence;
using Microsoft.UI.Xaml;
using Whiteboard.Interfaces;
using Whiteboard.Models;
using System.Text.Json;

namespace Whiteboard.Services;

public class CloudExport
{
    private readonly ICanvasRender _canvasRender;
    private IStorage? _storage;

    public CloudExport(ICanvasRender canvasRender, IStorage? storage = null)
    {
        _canvasRender = canvasRender ?? throw new ArgumentNullException(nameof(canvasRender));
        _storage = storage;
    }

    public async Task<bool> ExportToCloudAsync(UIElement element, string whiteboardId, string userId, bool isJpeg = false)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        _storage ??= await StorageFactory.CreateAsync(ModuleType.WhiteBoard);

        CanvasExportData exportData = await _canvasRender.CreateExportDataAsync(element, whiteboardId, isJpeg);

        return await SaveExportDataToCloudAsync(exportData, whiteboardId, userId);
    }

    public async Task<bool> SaveExportDataToCloudAsync(CanvasExportData exportData, string whiteboardId, string userId)
    {
        if (exportData == null)
        {
            throw new ArgumentNullException(nameof(exportData));
        }
        
        _storage ??= await StorageFactory.CreateAsync(ModuleType.WhiteBoard);

        var record = new
        {
            BoardId = whiteboardId,
            OwnerId = userId,
            FileName = exportData.FileName,
            ContentType = exportData.ContentType,
            Dimensions = $"{exportData.PixelWidth}x{exportData.PixelHeight}",
            ExportedAt = exportData.ExportDateTime,
            ImageData = exportData.ImageData
        };

        byte[] data = JsonSerializer.SerializeToUtf8Bytes(record);
        string key = $"{userId}:{whiteboardId}:{exportData.FileName}";

        return await _storage.Create(key, data);
    }
}