/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Services;

namespace Whiteboard.Test.Services;

public class CanvasRenderTests
{
    [Fact]
    public async Task RenderToPngAsync_NullElement_ThrowsArgumentNullException()
    {
        CanvasRender renderer = new();
        await Assert.ThrowsAsync<ArgumentNullException>(() => renderer.RenderToPngAsync(null!));
    }

    [Fact]
    public async Task RenderToJpegAsync_NullElement_ThrowsArgumentNullException()
    {
        CanvasRender renderer = new();
        await Assert.ThrowsAsync<ArgumentNullException>(() => renderer.RenderToJpegAsync(null!));
    }

    [Fact]
    public async Task CreateExportDataAsync_NullElement_ThrowsArgumentNullException()
    {
        CanvasRender renderer = new();
        await Assert.ThrowsAsync<ArgumentNullException>(() => renderer.CreateExportDataAsync(null!, "board1"));
    }
}
