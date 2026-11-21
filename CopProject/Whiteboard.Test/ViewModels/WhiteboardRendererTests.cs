/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Microsoft.UI.Xaml.Controls;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;
using Whiteboard.Test.Mocks;
using Whiteboard.Test.Views;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

[Collection("XamlUi")]
public class WhiteboardRendererTests(XamlUiThreadFixture ui)
{
    private static readonly Color s_black = Color.FromArgb(255, 0, 0, 0);
    private static readonly Color s_blue = Color.FromArgb(255, 0, 120, 215);
    private static readonly ToolbarInfo s_toolbar = new(s_black, 2, s_blue);

    [Fact]
    public async Task Constructor_ThrowsOnNullAndRendersExistingItems()
    {
        Canvas? canvas = null;
        WhiteboardRenderer? renderer = null;

        await ui.RunAsync(() => {
            MockWhiteboardClient client = new();
            canvas = new Canvas();
            MockCanvasPreview preview = new();
            TextboxState textboxState = new(client, preview, s_toolbar);
            SelectState selectState = new(client, preview, s_toolbar);

            Assert.Throws<ArgumentNullException>(() => new WhiteboardRenderer(null!, canvas, textboxState, selectState));
            Assert.Throws<ArgumentNullException>(() => new WhiteboardRenderer(client.Canvas, null!, textboxState, selectState));
            Assert.Throws<ArgumentNullException>(() => new WhiteboardRenderer(client.Canvas, canvas, null!, selectState));
            Assert.Throws<ArgumentNullException>(() => new WhiteboardRenderer(client.Canvas, canvas, textboxState, null!));

            Circle existing = new() { Center = new Point2D(50, 50), Radius = 15, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2 };
            client.Canvas.AddItem(existing);

            renderer = new WhiteboardRenderer(client.Canvas, canvas, textboxState, selectState);
        });

        await ui.RunAsync(() => {
            Assert.Single(canvas!.Children);
            renderer!.Dispose();
        });
    }

    [Fact]
    public async Task AddUpdateDelete_RendersAllNineItemTypes()
    {
        MockWhiteboardClient client = new();
        Canvas? canvas = null;
        WhiteboardRenderer? renderer = null;

        IItem[] items = [
            new Square { Center = new Point2D(50, 50), SideLegth = 20, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 1 },
            new Rectangle { Center = new Point2D(60, 60), Width = 40, Height = 20, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 2 },
            new Circle { Center = new Point2D(70, 70), Radius = 15, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 3 },
            new Ellipse { Center = new Point2D(80, 80), Width = 50, Height = 30, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 4 },
            new EqTriangle { Center = new Point2D(90, 90), SideLength = 30, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 5 },
            new Polygon { Vertices = [new Point2D(10, 10), new Point2D(40, 10), new Point2D(25, 40)], BorderColor = s_black, FillColor = s_blue, BorderWidth = 2, Zindex = 6 },
            new Line { Begin = new Point2D(10, 10), End = new Point2D(50, 50), Color = s_black, Width = 3, Zindex = 7 },
            new FreehandItem { Points = [new Point2D(5, 5), new Point2D(15, 25), new Point2D(30, 10)], StrokeColor = s_black, StrokeWidth = 2, Zindex = 8 },
            new Textbox { Center = new Point2D(100, 100), Width = 80, Height = 30, Content = "Hello", FontColor = s_black, FontSize = 14, Zindex = 9 }
        ];

        await ui.RunAsync(() => {
            canvas = new Canvas();
            MockCanvasPreview preview = new();
            TextboxState textboxState = new(client, preview, s_toolbar);
            SelectState selectState = new(client, preview, s_toolbar);
            renderer = new WhiteboardRenderer(client.Canvas, canvas, textboxState, selectState);

            foreach (IItem item in items)
            {
                client.Canvas.AddItem(item);
            }
        });

        await ui.RunAsync(() => {
            Assert.Equal(9, canvas!.Children.Count);

            foreach (IItem item in items)
            {
                item.MoveBy(5, 5);
                client.Canvas.EditItem(item);
            }
        });

        await ui.RunAsync(() => {
            Assert.Equal(9, canvas!.Children.Count);

            foreach (IItem item in items)
            {
                client.Canvas.DeleteItem(item.Id);
            }
        });

        await ui.RunAsync(() => {
            Assert.Empty(canvas!.Children);
            renderer!.Dispose();
        });
    }

    [Fact]
    public async Task SelectionOutlineAndDispose_UpdateAndDetachCleanly()
    {
        MockWhiteboardClient client = new();
        Canvas? canvas = null;
        SelectState? selectState = null;
        WhiteboardRenderer? renderer = null;
        Rectangle rect = new() { Center = new Point2D(50, 50), Width = 40, Height = 20, BorderColor = s_black, FillColor = s_blue, BorderWidth = 2 };

        await ui.RunAsync(() => {
            canvas = new Canvas();
            MockCanvasPreview preview = new();
            TextboxState textboxState = new(client, preview, s_toolbar);
            selectState = new SelectState(client, preview, s_toolbar);
            renderer = new WhiteboardRenderer(client.Canvas, canvas, textboxState, selectState);
            client.Canvas.AddItem(rect);
        });

        await ui.RunAsync(() => {
            Assert.Single(canvas!.Children);
            selectState!.PointerPressed(new Point2D(50, 50));
            selectState.PointerReleased(new Point2D(50, 50));
        });

        await ui.RunAsync(() => {
            Assert.True(canvas!.Children.Count >= 2);
            rect.MoveBy(10, 10);
            client.Canvas.EditItem(rect);
        });

        await ui.RunAsync(() => {
            Assert.True(canvas!.Children.Count >= 2);
            selectState!.ClearSelection();
        });

        await ui.RunAsync(() => {
            Assert.Single(canvas!.Children);
            client.Canvas.AddItem(new UnknownItem());
            renderer!.Dispose();
            client.Canvas.AddItem(new Circle { Center = new Point2D(10, 10), Radius = 5, BorderColor = s_black, FillColor = s_blue, BorderWidth = 1 });
        });

        await ui.RunAsync(() => {
            Assert.Equal(2, canvas!.Children.Count);
        });
    }

    [Fact]
    public async Task AddItem_DispatchesWhenCalledFromBackgroundThread()
    {
        MockWhiteboardClient client = new();
        Canvas? canvas = null;
        WhiteboardRenderer? renderer = null;

        await ui.RunAsync(() => {
            canvas = new Canvas();
            MockCanvasPreview preview = new();
            TextboxState textboxState = new(client, preview, s_toolbar);
            SelectState selectState = new(client, preview, s_toolbar);
            renderer = new WhiteboardRenderer(client.Canvas, canvas, textboxState, selectState);
        });

        await Task.Run(() => client.Canvas.AddItem(new Circle { Center = new Point2D(20, 20), Radius = 10, BorderColor = s_black, FillColor = s_blue, BorderWidth = 1 }));

        await ui.RunAsync(() => {
            Assert.Single(canvas!.Children);
            renderer!.Dispose();
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
