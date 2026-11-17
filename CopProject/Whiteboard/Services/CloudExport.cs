/// Author : Karthik Krishna M
/// Roll Number : 112301014
/// LICENSE : MIT

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Cloud.Persistence;
using Microsoft.UI.Xaml;
using Whiteboard.Interfaces;
using Whiteboard.Models;

namespace Whiteboard.Services;

public class CloudExport
{
    private readonly ICanvasRender _canvasRender;
    private IStorage? _storage;

    public CloudExport(ICanvasRender canvasRender, IStorage? storage = null)
    {
        ArgumentNullException.ThrowIfNull(canvasRender);
        _canvasRender = canvasRender;
        _storage = storage;
    }

    public async Task<bool> ExportToCloudAsync(UIElement element, string whiteboardId, string userId, bool isJpeg = false)
    {
        ArgumentNullException.ThrowIfNull(element);
        Trace.TraceInformation($"[Whiteboard][CloudExport] ExportToCloudAsync: WhiteboardId={whiteboardId}, UserId={userId}, isJpeg={isJpeg}.");

        _storage ??= await StorageFactory.CreateAsync(ModuleType.WhiteBoard);

        CanvasExportData exportData = await _canvasRender.CreateExportDataAsync(element, whiteboardId, isJpeg);

        return await SaveExportDataToCloudAsync(exportData, whiteboardId, userId);
    }

    public async Task<bool> SaveExportDataToCloudAsync(CanvasExportData exportData, string whiteboardId, string userId)
    {
        ArgumentNullException.ThrowIfNull(exportData);

        _storage ??= await StorageFactory.CreateAsync(ModuleType.WhiteBoard);

        var record = new {
            BoardId = whiteboardId,
            OwnerId = userId,
            exportData.FileName,
            exportData.ContentType,
            Dimensions = $"{exportData.PixelWidth}x{exportData.PixelHeight}",
            ExportedAt = exportData.ExportDateTime,
            exportData.ImageData
        };

        byte[] data = JsonSerializer.SerializeToUtf8Bytes(record);
        string key = $"{userId}:{whiteboardId}:{exportData.FileName}";
        Trace.TraceInformation($"[Whiteboard][CloudExport] Saving export data to Cloud with Key={key} ({data.Length} bytes).");

        bool success = await _storage.Create(key, data);
        if (success)
        {
            Trace.TraceInformation($"[Whiteboard][CloudExport] Successfully saved export data to Cloud: Key={key}.");
        }
        else
        {
            Trace.TraceError($"[Whiteboard][CloudExport][Error] Failed to save export data to Cloud: Key={key}.");
        }

        return success;
    }
}
