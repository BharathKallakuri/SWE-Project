using System.Drawing;

namespace ScreenShare;

public interface IImageStitcher
{
    ///<summary> 
    /// stores complete screen image. for first screen capture or when sender decides to send full frame.
    ///</summary>
    void ApplyFullFrame(Bitmap frame);

    ///<summary> 
    /// updates specific region in the Frame using Tile's position,dimension and image data.
    ///</summary>
    void ApplyChangedTile(Tile tile);

    ///<summary> 
    /// returns the new constructed Frame or null if not frame is recieved
    ///</summary>
    Bitmap? GetCurrentFrame();

    ///<summary> 
    /// for cleaning purpose at end of stream or restart
    ///</summary>
    void Reset();
}
