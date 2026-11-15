// Author = Aryan
// Roll Number = 112301041
using System.Drawing;

namespace ScreenShare;

public interface IImageSource
{
    Bitmap Capture(); // current image capture whether from video or screen
}
