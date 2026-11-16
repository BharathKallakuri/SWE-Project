/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Models;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

public class WhiteboardRendererTests
{
    private readonly WhiteboardCanvas _board = new();
    private readonly MockWhiteboardClient _client = new();
    private readonly MockCanvasPreview _preview = new();
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 0, 0, 0), 2.0, Color.FromArgb(255, 255, 255, 255));

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        TextboxState textboxState = new(_client, _preview, _toolbar);
        SelectState selectState = new(_client, _preview, _toolbar);

        Assert.Throws<ArgumentNullException>(() => new WhiteboardRenderer(null!, null!, textboxState, selectState));
        Assert.Throws<ArgumentNullException>(() => new WhiteboardRenderer(_board, null!, textboxState, selectState));
        Assert.Throws<ArgumentNullException>(() => new WhiteboardRenderer(_board, null!, null!, selectState));
        Assert.Throws<ArgumentNullException>(() => new WhiteboardRenderer(_board, null!, textboxState, null!));
    }
}
