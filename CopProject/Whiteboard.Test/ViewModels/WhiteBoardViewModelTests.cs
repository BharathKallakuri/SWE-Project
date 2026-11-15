/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Services;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;

namespace Whiteboard.Test.ViewModels;

public class WhiteBoardViewModelTests
{
    private readonly MockWhiteboardClient _client = new();

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => new WhiteBoardViewModel((IWhiteboardClient)null!, "session-1"));
        Assert.Throws<ArgumentException>(() => new WhiteBoardViewModel(_client, ""));
        Assert.Throws<ArgumentNullException>(() => new WhiteBoardViewModel((WhiteboardSession)null!));
    }

    [Fact]
    public void InitialProperties_HaveExpectedDefaults()
    {
        using WhiteBoardViewModel vm = new(_client, "session-1");

        Assert.Equal(WhiteboardTool.Pen, vm.SelectedTool);
        Assert.NotNull(vm.ShapeRecognitionService);
        Assert.False(vm.CanUndo);
        Assert.False(vm.CanRedo);

        vm.IsSmartShapeEnabled = true;
        Assert.True(vm.IsSmartShapeEnabled);
    }

    [Fact]
    public void UndoAndRedo_ForwardsToClient()
    {
        using WhiteBoardViewModel vm = new(_client, "session-1");

        // Client has Undo and Redo methods that we can invoke safely through VM
        vm.Undo();
        vm.Redo();
    }

    [Fact]
    public void UninitializedMethods_ThrowInvalidOperationException()
    {
        using WhiteBoardViewModel vm = new(_client, "session-1");

        Assert.Throws<InvalidOperationException>(() => vm.SelectTool(WhiteboardTool.Line));
        Assert.Throws<InvalidOperationException>(vm.ResetCurrentState);
        Assert.Throws<InvalidOperationException>(() => vm.PointerPressed(new Point2D(0, 0)));
        Assert.Throws<InvalidOperationException>(() => vm.PointerMoved(new Point2D(0, 0)));
        Assert.Throws<InvalidOperationException>(() => vm.PointerReleased(new Point2D(0, 0)));
        Assert.Throws<InvalidOperationException>(() => _ = vm.HandlesPointerOnTextbox);
    }

    [Fact]
    public void Dispose_CanBeCalledWithoutThrowing()
    {
        WhiteBoardViewModel vm = new(_client, "session-1");
        vm.Dispose();
        vm.Dispose();
    }
}
