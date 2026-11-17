/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Views;

namespace Whiteboard.Test.Views;

public class CanvasPreviewTests
{
    [Fact]
    public void Constructor_NullCanvas_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CanvasPreview(null!));
    }
}
