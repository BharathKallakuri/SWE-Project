// Author = Aryan
// Roll Number = 112301041

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using ScreenShare;

namespace Screenshare.Tests;

public class JpegFullFrameTests
{
    private static readonly byte[] s_jpegStart = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] s_pngStart = { 0x89, 0x50, 0x4E, 0x47 };

    // a screen-like picture: lines of text on white
    private static Bitmap TextScreen()
    {
        Bitmap bitmap = new(640, 360);
        using Graphics g = Graphics.FromImage(bitmap);
        g.Clear(Color.White);
        using Font font = new("Consolas", 10);
        for (int line = 0; line < 22; line++)
        {
            g.DrawString($"{line,3}  var frame{line} = service.ProcessFrame(); // step {line * 7}", font, Brushes.Black, 4, 4 + line * 16);
        }
        return bitmap;
    }

    // a screen like a real app: gradients, coloured panels and ClearType text, where JPEG wins
    private static Bitmap ColourfulScreen()
    {
        Bitmap bitmap = new(640, 360);
        using Graphics g = Graphics.FromImage(bitmap);
        using (LinearGradientBrush sky = new(new Rectangle(0, 0, 640, 360), Color.SteelBlue, Color.MistyRose, 35f))
        {
            g.FillRectangle(sky, 0, 0, 640, 360);
        }
        Random random = new(7);
        for (int i = 0; i < 40; i++)
        {
            using SolidBrush brush = new(Color.FromArgb(160, random.Next(256), random.Next(256), random.Next(256)));
            g.FillEllipse(brush, random.Next(600), random.Next(320), 20 + random.Next(120), 20 + random.Next(90));
        }
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        using Font font = new("Segoe UI", 11);
        for (int line = 0; line < 12; line++)
        {
            g.DrawString($"Slide {line}: quarterly numbers and the plan for the next release", font, Brushes.Black, 12, 12 + line * 28);
        }
        return bitmap;
    }

    private static bool StartsWith(byte[] data, byte[] prefix) { return data.Take(prefix.Length).SequenceEqual(prefix); }

    [Fact]
    public void Encode_GivesJpegOfSameSize()
    {
        using Bitmap screen = TextScreen();

        byte[] jpeg = JpegEncoder.Encode(screen, 75);

        Assert.True(StartsWith(jpeg,s_jpegStart));
        using MemoryStream stream = new(jpeg);
        using Bitmap decoded = new(stream);
        Assert.Equal((640, 360), (decoded.Width, decoded.Height));
    }

    [Fact]
    public void Encode_LowerQuality_IsSmaller()
    {
        using Bitmap screen = TextScreen();

        Assert.True(JpegEncoder.Encode(screen, 40).Length < JpegEncoder.Encode(screen, 90).Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Encode_QualityOutOfRange_Throws(int quality)
    {
        using Bitmap screen = TextScreen();

        Assert.Throws<ArgumentOutOfRangeException>(() => JpegEncoder.Encode(screen, quality));
    }

    [Fact]
    public void Encode_NullBitmap_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => JpegEncoder.Encode(null!, 75));
    }

    // the packetizer picks the format for full frames

    private static ScreenDataPacket FullFramePacket(ScreenPacketizer packetizer, Bitmap screen)
    {
        using ImageDiffResult full = ImageDiffResult.CreateFullFrame(screen);
        return packetizer.CreatePacket(full, DateTime.UtcNow);
    }

    [Fact]
    public void Packetizer_ColourfulScreen_SendsSmallerJpeg()
    {
        using Bitmap screen = ColourfulScreen();
        int png = PngEncoder.Encode(screen).Length;

        ScreenDataPacket packet = FullFramePacket(new ScreenPacketizer("aryan", jpegQuality: 75), new Bitmap(screen));

        Assert.True(StartsWith(packet.FullFrameData!, s_jpegStart));
        Assert.True(packet.FullFrameData!.Length < png / 2, $"JPEG {packet.FullFrameData.Length} bytes, PNG {png} bytes");
    }

    [Fact]
    public void Packetizer_AfterJpegWins_SkipsPngUntilRecheck()
    {
        ScreenPacketizer packetizer = new("aryan", jpegQuality: 75);
        FullFramePacket(packetizer, ColourfulScreen());   // JPEG wins

        // text would be smaller as PNG, but PNG isn't tried again until the recheck
        for (int i = 0; i < ScreenPacketizer.PngRecheckInterval; i++)
        {
            Assert.True(StartsWith(FullFramePacket(packetizer, TextScreen()).FullFrameData!,s_jpegStart));
        }

        Assert.True(StartsWith(FullFramePacket(packetizer, TextScreen()).FullFrameData!, s_pngStart));
    }

    [Fact]
    public void Packetizer_PlainTextOnWhite_KeepsSmallerPng()
    {
        // JPEG is bigger than PNG for crisp text on a flat background
        ScreenDataPacket packet = FullFramePacket(new ScreenPacketizer("aryan", jpegQuality: 75), TextScreen());

        Assert.True(StartsWith(packet.FullFrameData!, s_pngStart));
    }

    [Fact]
    public void Packetizer_WithoutQuality_SendsPngFullFrames()
    {
        ScreenDataPacket packet = FullFramePacket(new ScreenPacketizer("aryan"), ColourfulScreen());

        Assert.True(StartsWith(packet.FullFrameData!, s_pngStart));
    }

    // a big change sent as tiles can be smaller as one JPEG

    private static ScreenPacketizer StartedPacketizer(int jpegQuality)
    {
        ScreenPacketizer packetizer = new("aryan", jpegQuality);
        FullFramePacket(packetizer, ColourfulScreen());   // deltas need a first full frame
        return packetizer;
    }

    private static Tile TileOfBytes(int bytes) { return new(0, 0, 64, 64, 1, new byte[bytes]); }

    [Fact]
    public void BigDelta_SmallerAsJpeg_IsSentAsOneFullFrame()
    {
        ScreenPacketizer packetizer = StartedPacketizer(75);
        using ImageDiffResult delta = ImageDiffResult.Delta(new List<Tile> { TileOfBytes(400_000) }, ColourfulScreen());

        ScreenDataPacket packet = packetizer.CreatePacket(delta, DateTime.UtcNow);

        Assert.True(packet.IsFullFrame);
        Assert.Equal(1, packet.FrameNumber);
        Assert.True(StartsWith(packet.FullFrameData!,s_jpegStart));
    }

    [Fact]
    public void BigDelta_SmallerAsTiles_StaysTiles()
    {
        ScreenPacketizer packetizer = StartedPacketizer(75);
        using ImageDiffResult delta = ImageDiffResult.Delta(new List<Tile> { TileOfBytes(200) }, ColourfulScreen());

        ScreenDataPacket packet = packetizer.CreatePacket(delta, DateTime.UtcNow);

        Assert.Equal(DataPacketType.ChangedTiles, packet.Type);
    }

    [Fact]
    public void BigDelta_WithoutJpeg_StaysTiles()
    {
        ScreenPacketizer packetizer = StartedPacketizer(0);
        using ImageDiffResult delta = ImageDiffResult.Delta(new List<Tile> { TileOfBytes(400_000) }, ColourfulScreen());

        ScreenDataPacket packet = packetizer.CreatePacket(delta, DateTime.UtcNow);

        Assert.Equal(DataPacketType.ChangedTiles, packet.Type);
    }

    [Fact]
    public void Differ_AttachesWholePicture_OnlyToBigDeltas()
    {
        ImageDiffer differ = new();
        using Bitmap before = ColourfulScreen();
        using Bitmap smallChange = new(before);
        using (Graphics g = Graphics.FromImage(smallChange)){ g.FillRectangle(Brushes.Red, 0, 0, 10, 10); }
        using Bitmap bigChange = new(before);
        using (Graphics g = Graphics.FromImage(bigChange))
        {
            // a third of the picture changes: still tiles, but well over 16 KB of them
            Random random = new(3);
            for (int i = 0; i < 400; i++)
            {
                using SolidBrush brush = new(Color.FromArgb(random.Next(256), random.Next(256), random.Next(256)));
                g.FillRectangle(brush, random.Next(620), random.Next(110), 8, 8);
            }
        }

        using ImageDiffResult small = differ.Compare(before, smallChange);
        using ImageDiffResult big = differ.Compare(before, bigChange);

        Assert.Null(small.WholeFrame);
        Assert.False(big.IsFullFrame);
        Assert.NotNull(big.WholeFrame);
        Assert.True(big.ChangedTiles.Sum(t => t.Data.Length) > ImageDiffer.LARGE_DELTA_BYTES);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Packetizer_QualityOutOfRange_Throws(int quality)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScreenPacketizer("aryan", quality));
    }

    [Fact]
    public void Viewer_DecodesJpegFullFrame()
    {
        ScreenDataPacket packet = FullFramePacket(new ScreenPacketizer("aryan", jpegQuality: 75), ColourfulScreen());
        Assert.True(StartsWith(packet.FullFrameData!, s_jpegStart));
        using ImageStitcher stitcher = new();

        PacketApplyOutcome outcome = new ScreenPacketApplier("aryan", stitcher).Apply(packet);

        Assert.Equal(PacketApplyOutcome.Applied, outcome);
        using Bitmap shown = stitcher.GetCurrentFrame()!;
        Assert.Equal((640, 360), (shown.Width, shown.Height));
    }

}
