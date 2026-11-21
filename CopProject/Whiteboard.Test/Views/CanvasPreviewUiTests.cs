/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Microsoft.UI.Xaml.Controls;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;
using Whiteboard.Views;
using Windows.UI;

namespace Whiteboard.Test.Views;

[Collection("XamlUi")]
public class CanvasPreviewUiTests(XamlUiThreadFixture ui)
{
    private static readonly Color s_black = Color.FromArgb(255, 0, 0, 0);
    private static readonly Color s_blue = Color.FromArgb(255, 0, 120, 215);

    [Fact]
    public async Task ShowPreview_RendersAllSupportedItemTypesAndReplacesPrevious()
    {
        await ui.RunAsync(() => {
            Canvas canvas = new();
            CanvasPreview preview = new(canvas);

            IItem[] items = [
                new Square { Center = new Point2D(50, 50), SideLegth = 20, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 1 },
                new Rectangle { Center = new Point2D(60, 60), Width = 40, Height = 20, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 2 },
                new Circle { Center = new Point2D(70, 70), Radius = 15, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 3 },
                new Ellipse { Center = new Point2D(80, 80), Width = 50, Height = 30, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 4 },
                new EqTriangle { Center = new Point2D(90, 90), SideLength = 30, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 5 },
                new Polygon { Vertices = [new Point2D(10, 10), new Point2D(40, 10), new Point2D(25, 40)], BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 6 },
                new Line { Begin = new Point2D(10, 10), End = new Point2D(50, 50), Color = s_black, Width = 3, Zindex = 7 },
                new FreehandItem { Points = [new Point2D(5, 5), new Point2D(15, 25), new Point2D(30, 10)], StrokeColor = s_black, StrokeWidth = 2, Zindex = 8 }
            ];

            foreach (IItem item in items)
            {
                preview.ShowPreview(item);
                Assert.Single(canvas.Children);
                Assert.Equal((int)item.Zindex, Canvas.GetZIndex(canvas.Children[0]));
            }

            preview.ClearPreview();
            Assert.Empty(canvas.Children);

            // Second ClearPreview is a no-op
            preview.ClearPreview();
            Assert.Empty(canvas.Children);
        });
    }

    [Fact]
    public async Task ShowPreview_HandlesNullEmptyCollectionsAndUnknownItems()
    {
        await ui.RunAsync(() => {
            Canvas canvas = new();
            CanvasPreview preview = new(canvas);

            Assert.Throws<ArgumentNullException>(() => preview.ShowPreview(null!));

            preview.ShowPreview(new UnknownItem());
            Assert.Empty(canvas.Children);

            preview.ShowPreview(new Polygon { Vertices = [] });
            preview.ShowPreview(new Polygon { Vertices = [new Point2D(10, 10), new Point2D(40, 10)], BorderColor = s_black, BorderWidth = 2 });
            Assert.Single(canvas.Children);
            preview.ShowPreview(new FreehandItem { Points = [] });
            preview.ClearPreview();
            Assert.Empty(canvas.Children);
        });
    }

    [Fact]
    public async Task ShowPreview_DispatchesWhenCalledFromBackgroundThread()
    {
        Canvas? canvas = null;
        CanvasPreview? preview = null;

        await ui.RunAsync(() => {
            canvas = new Canvas();
            preview = new CanvasPreview(canvas);
        });

        // Call from non-UI thread to exercise DispatcherQueue.TryEnqueue branch
        await Task.Run(() => preview!.ShowPreview(new Circle { Center = new Point2D(20, 20), Radius = 10, BorderColor = s_black, FillColor = s_blue, BorderWidth = 1 }));

        await ui.RunAsync(() => {
            Assert.Single(canvas!.Children);
            preview!.ClearPreview();
            Assert.Empty(canvas.Children);
        });
    }

    private sealed class UnknownItem : Item
    {
        public override Rect2D BoundingBox => new(0, 0, 0, 0);
        public override bool HitTest(Point2D point)
        {
            return false;
        }

        public override void MoveBy(double dx, double dy) { }
    }
}
