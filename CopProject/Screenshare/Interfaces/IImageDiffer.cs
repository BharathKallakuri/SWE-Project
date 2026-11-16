// Author = Aryan
// Roll Number = 112301041

using System.Drawing;

namespace ScreenShare;

public interface IImageDiffer
{
    ImageDiffResult Compare(Bitmap previousFrame, Bitmap currentFrame);
}
