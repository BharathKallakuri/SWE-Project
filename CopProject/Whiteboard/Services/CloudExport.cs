using System;
using System.Threading.Tasks;
using Cloud.Persistence;
using Microsoft.UI.Xaml;
using Whiteboard.Interfaces;
using Whiteboard.Models;

namespace Whiteboard.Services;

public class CloudExport 
{
    private readonly ICanvasRender _canvasRender;
    private readonly IStorage _storage;

    public CloudExport(ICanvasRender canvasRender, IStorage storage = null)
    {
        _canvasRender = canvasRender ?? throw new ArgumentNullException(nameof(canvasRender));
        _storage = storage ?? StorageFactory.CreateStorage();
    }

    public async Task<bool> ExportToCloudAsync(UIElement element, string whiteboardId, string userId, bool isJpeg = false)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        CanvasExportData exportData = await _canvasRender.CreateExportDataAsync(element, whiteboardId, isJpeg);

        var record = new PersistenceItem
        {
            Id = exportData.FileName,
            PartitionKey = userId,
            ModuleType = ModuleTypes.Whiteboard,
            Data = new
            {
                BoardId = whiteboardId,
                OwnerId = userId,
                FileName = exportData.FileName,
                ContentType = exportData.ContentType,
                Dimensions = $"{exportData.PixelWidth}x{exportData.PixelHeight}",
                ExportedAt = exportData.ExportDateTime,
                ImageBase64 = Convert.ToBase64String(exportData.ImageData)
            }
        };

        return await _storage.SaveAsync(record);
    }
}