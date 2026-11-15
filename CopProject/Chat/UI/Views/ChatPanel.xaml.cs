// Author: Rajat
// Roll no.: 112301044

using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Chat;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UI.ViewModels;
using Windows.System;
using Windows.UI;

namespace UI.Views;

public sealed partial class ChatPanel : UserControl
{
    private ChatPanelViewModel? _viewModel;
    private ScrollViewer? _mainScrollViewer, _threadScrollViewer;
    private bool _mainNearBottom = true, _threadNearBottom = true;
    private MessageViewModel? _pendingReactionMessage;
    private Button? _reactionAnchor;
    private int _emojiInputVersion;

    private const uint InputKeyboard = 1;
    private const uint KeyUp = 0x0002;
    private const ushort VkLeftWindows = 0x5B;
    private const ushort VkPeriod = 0xBE;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, NativeInput[] inputs, int inputSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInput
    {
        public uint _type;
        public InputUnion _data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public NativeKeyboardInput _keyboard;
        [FieldOffset(0)] public NativeMouseInput _mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeKeyboardInput
    {
        public ushort _virtualKey;
        public ushort _scanCode;
        public uint _flags;
        public uint _time;
        public nuint _extraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMouseInput
    {
        public int _dx, _dy;
        public uint _mouseData, _flags, _time;
        public nuint _extraInfo;
    }

    public ChatPanel()
    {
        InitializeComponent();
    }

    public async void ShowChatServerStartupError()
    {
        if (XamlRoot is null)
        {
            return;
        }

        ContentDialog dialog = new() {
            Title = "Chat unavailable",
            Content = ChatServer.StartupFailureMessage,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }

    public ChatPanelViewModel? ViewModel
    {
        get => _viewModel;
        set {
            if (ReferenceEquals(_viewModel, value))
            {
                return;
            }

            _viewModel?.ScrollToMessageRequested -= OnScrollToMessageRequested;

            _viewModel = value;
            DataContext = value;
            SetFlyoutDataContext(ParticipantsButton.Flyout, value);
            SetFlyoutDataContext(MentionButton.Flyout, value);
            SetFlyoutDataContext(ThreadMentionButton.Flyout, value);

            if (_viewModel is not null)
            {
                _viewModel.Messages.CollectionChanged += OnMessagesChanged;
                _viewModel.ThreadMessages.CollectionChanged += OnThreadMessagesChanged;
                _viewModel.ScrollToMessageRequested += OnScrollToMessageRequested;
                ScrollMainConversationToLatest();
            }
        }
    }

    private static void SetFlyoutDataContext(object? flyout, object? dataContext)
    {
        if (flyout is Flyout popup && popup.Content is FrameworkElement content)
        {
            content.DataContext = dataContext;
        }
    }

    private void MessagesList_Loaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        _mainScrollViewer = FindChild<ScrollViewer>(MessagesList);
        _mainScrollViewer?.ViewChanged += MainScrollViewer_ViewChanged;

        ScrollMainConversationToLatest();
    }

    private void ThreadMessagesList_Loaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        _threadScrollViewer = FindChild<ScrollViewer>(ThreadMessagesList);
        _threadScrollViewer?.ViewChanged += ThreadScrollViewer_ViewChanged;

        ScrollThreadToLatest();
    }

    private void MainScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_mainScrollViewer is not null)
        {
            _mainNearBottom = _mainScrollViewer.ScrollableHeight - _mainScrollViewer.VerticalOffset < 90;
        }
    }

    private void ThreadScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_threadScrollViewer is not null)
        {
            _threadNearBottom = _threadScrollViewer.ScrollableHeight - _threadScrollViewer.VerticalOffset < 90;
        }
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            ScrollMainConversationToLatest();
        }
    }

    private void OnThreadMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            ScrollThreadToLatest();
        }
    }

    private void ScrollMainConversationToLatest()
    {
        ChatPanelViewModel? viewModel = ViewModel;
        if (viewModel is null || viewModel.Messages.Count == 0)
        {
            return;
        }

        if (_mainScrollViewer is null)
        {
            return;
        }

        if (viewModel.Messages.LastOrDefault() is MessageViewModel latest)
        {
            MessagesList.ScrollIntoView(latest);
        }

        MessagesList.UpdateLayout();
        _mainScrollViewer.ChangeView(null, _mainScrollViewer.ScrollableHeight, null);
    }

    private void ScrollThreadToLatest()
    {
        ChatPanelViewModel? viewModel = ViewModel;
        if (viewModel is null || viewModel.ThreadMessages.Count == 0)
        {
            return;
        }

        if (_threadScrollViewer is null)
        {
            return;
        }

        if (viewModel.ThreadMessages.LastOrDefault() is MessageViewModel latest)
        {
            ThreadMessagesList.ScrollIntoView(latest);
        }

        ThreadMessagesList.UpdateLayout();
        _threadScrollViewer.ChangeView(null, _threadScrollViewer.ScrollableHeight, null);
    }

    private async void OnScrollToMessageRequested(Guid messageId)
    {
        if (ViewModel?.Messages.FirstOrDefault(message => message.Id == messageId) is not MessageViewModel target)
        {
            return;
        }

        MessagesList.ScrollIntoView(target);
        for (int pulse = 0; pulse < 4; pulse++)
        {
            target.SetJumpHighlighted(pulse % 2 == 0);
            await Task.Delay(240);
        }
        target.SetJumpHighlighted(false);
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
            {
                return found;
            }

            if (FindChild<T>(child) is T nested)
            {
                return nested;
            }
        }
        return null;
    }

    private static bool OpenWindowsEmojiPicker()
    {
        // Win + . is the Windows 10/11 native emoji picker shortcut.
        NativeInput[] inputs =
        {
            KeyboardInput(VkLeftWindows),
            KeyboardInput(VkPeriod),
            KeyboardInput(VkPeriod, KeyUp),
            KeyboardInput(VkLeftWindows, KeyUp)
        };
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInput>()) == (uint)inputs.Length;
    }

    private static NativeInput KeyboardInput(ushort key, uint flags = 0)
    {
        return new() {
            _type = InputKeyboard,
            _data = new InputUnion {
                _keyboard = new NativeKeyboardInput { _virtualKey = key, _flags = flags }
            }
        };
    }

    private async void NativeEmojiInput_TextChanged(object _, TextChangedEventArgs __)
    {
        if (_pendingReactionMessage is null || string.IsNullOrWhiteSpace(NativeEmojiInput.Text))
        {
            return;
        }

        // The emoji panel can deliver a grapheme in multiple text updates (for example,
        // a base emoji followed by a variation selector). Wait for the input to settle.
        int version = ++_emojiInputVersion;
        await Task.Delay(140);
        if (version != _emojiInputVersion || _pendingReactionMessage is null)
        {
            return;
        }

        string emoji = NativeEmojiInput.Text.Trim();
        if (!IsSingleEmoji(emoji))
        {
            return;
        }

        MessageViewModel message = _pendingReactionMessage;
        _pendingReactionMessage = null;
        _emojiInputVersion++;
        NativeEmojiInput.Text = string.Empty;
        message.ToggleReactionCommand.Execute(emoji);
        _reactionAnchor?.Focus(FocusState.Programmatic);
        _reactionAnchor = null;
    }

    private static bool IsSingleEmoji(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || new StringInfo(value).LengthInTextElements != 1)
        {
            return false;
        }

        foreach (Rune rune in value.EnumerateRunes())
        {
            if ((rune.Value >= 0x2300 && rune.Value <= 0x27FF) || rune.Value >= 0x1F000)
            {
                return true;
            }
        }

        return false;
    }

    private void MessageFlyoutButton_Loaded(object sender, RoutedEventArgs e)
    {
        _ = e;
        if (sender is not Button button)
        {
            return;
        }

        if (button.Flyout is MenuFlyout menu)
        {
            foreach (object item in menu.Items)
            {
                if (item is FrameworkElement element)
                {
                    element.DataContext = button.DataContext;
                }
            }
        }
        else if (button.Flyout is Flyout flyout && flyout.Content is FrameworkElement content)
        {
            content.DataContext = button.DataContext;
        }
    }

    private void MessageInput_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        _ = sender;
        if (e.Key != VirtualKey.Enter || ViewModel is null)
        {
            return;
        }

        ViewModel.SendMessageCommand.Execute(null);
        e.Handled = true;
    }

    private void ThreadMessageInput_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        _ = sender;
        if (e.Key != VirtualKey.Enter || ViewModel is null)
        {
            return;
        }

        ViewModel.SendThreadReplyCommand.Execute(null);
        e.Handled = true;
    }
}
