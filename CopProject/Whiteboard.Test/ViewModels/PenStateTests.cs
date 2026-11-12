/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Microsoft.UI;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;

namespace Whiteboard.Test.ViewModels;

/// <summary>
/// Pen state tests
/// </summary>
public class PenStateTests
{
    /// <summary>
    /// Mock whiteboard client
    /// </summary>
    private readonly MockWhiteboardClient _client = new();

    /// <summary>
    /// Mock canvas previewer
    /// </summary>
    private readonly MockCanvasPreview _preview = new();

    /// <summary>
    /// A random toolbar
    /// </summary>
    private readonly ToolbarInfo _toolbar = new(Colors.Black, 3, Colors.Red);

    /// <summary>
    /// Assert that constructor throws <see cref="ArgumentNullException"/> if any of the arguments is null
    /// </summary>
    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PenState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new PenState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new PenState(_client, _preview, null!));
    }
}

