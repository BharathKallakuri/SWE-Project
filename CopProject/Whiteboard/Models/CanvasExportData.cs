/// Author : Karthik Krishna M
/// Roll Number : 112301014
/// LICENSE : MIT

using System;
using System.Diagnostics.CodeAnalysis;

namespace Whiteboard.Models;

public class CanvasExportData
{
    public byte[] ImageData { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public DateTime ExportDateTime { get; set; }
}
