using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using UX.Models;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace UX;
public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _records = new() { "Weekly Review - Mon, 28 Sep - 45 min", "Patrol Planning - Fri, 25 Sep - 30 min", "Case Briefing - Wed, 23 Sep - 1 hr 10 min" };
    private string _userId = "";
    private string _userEmail = "";
    private string _picture = "";
    private string? _pickedPicture;
    private string _meetingId = "";
    private string _meetingPort = "";
    private bool _inMeeting;
    private bool _isHost;
    private bool _resizingChat;

    private string Role => _isHost ? "Host" : "Participant";

    public MainWindow()
    {
        InitializeComponent();
        Title = "SecureMeet";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        RecordsList.ItemsSource = _records;

        ShowScreen("login");
    }
    private static Visibility Vis(bool show)
    {
        return show ? Visibility.Visible : Visibility.Collapsed;
    }
    private void ShowScreen(string screen)
    {
        LoginView.Visibility = Vis(screen == "login");
        DashView.Visibility = Vis(screen == "dash");
        MeetingView.Visibility = Vis(screen == "meeting");
        UserPanel.Visibility = Vis(screen != "login");
        MeetingInfoText.Visibility = Vis(_inMeeting);
    }

    private ContentDialog MakeDialog(string title, object content, string okText)
    {
        return new ContentDialog { Title = title, Content = content, PrimaryButtonText = okText, CloseButtonText = "Cancel", XamlRoot = Root.XamlRoot };
    }

    // shows a picture (file path or web address) in a PersonPicture
    private static void SetPicture(PersonPicture pic, string? source)
    {
        pic.ProfilePicture = string.IsNullOrEmpty(source) ? null : new BitmapImage(new Uri(source));
    }

    // lets the person choose a picture file and returns its path (null if they cancel)
    private async Task<string?> PickPictureAsync()
    {
        var picker = new FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    // shows the name, picture and greeting of the signed-in person
    private void ShowProfile()
    {
        ProfileName.Text = _inMeeting ? _userId + " (" + Role + ")" : _userId;
        ProfilePic.DisplayName = _userId;
        SetPicture(ProfilePic, _picture);

        string greeting = "Good Evening";
        if (DateTime.Now.Hour < 12)
        {
            greeting = "Good Morning";
        }
        else if (DateTime.Now.Hour < 17)
        {
            greeting = "Good Afternoon";
        }
        GreetingText.Text = greeting + ", " + _userId + "!";
    }

    // login
    private async void PickLoginPicture_Click(object sender, RoutedEventArgs e)
    {
        string? path = await PickPictureAsync();
        if (path != null)
        {
            _pickedPicture = path;
            SetPicture(LoginPic, path);
        }
    }

    // opens the browser so the person can log in with their Google account
    private async void SignIn_Click(object sender, RoutedEventArgs e)
    {
        SignInBtn.IsEnabled = false;
        SignInBtn.Content = "Waiting for Google...";
        LoginError.Visibility = Visibility.Collapsed;

        try
        {
            string typedId = UserIdBox.Text.Trim();
            GoogleUser user = await GoogleAuth.SignInAsync();
            Activate();
            SavedProfile? saved = ProfileStore.Load(user.Email);
            _userEmail = user.Email;
            _userId = typedId.Length > 0 ? typedId : (saved?.UserId ?? user.Name);
            string savedPicture = saved?.PicturePath ?? "";
            if (_pickedPicture != null)
            {
                savedPicture = ProfileStore.CopyPicture(_pickedPicture);
            }
            if (!File.Exists(savedPicture))
            {
                savedPicture = "";
            }
            _picture = savedPicture.Length > 0 ? savedPicture : user.Picture;
            ProfileStore.Save(user.Email, _userId, savedPicture);

            ShowProfile();
            ShowScreen("dash");
        }
        catch (Exception ex)
        {
            LoginError.Text = "Google sign-in failed: " + ex.Message;
            LoginError.Visibility = Visibility.Visible;
        }
        finally
        {
            SignInBtn.IsEnabled = true;
            SignInBtn.Content = "Sign in with Google";
        }
    }
    private async void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        string? newPicture = null;
        var pic = new PersonPicture { Width = 80, Height = 80 };
        SetPicture(pic, _picture);
        var changePic = new Button { Content = "Change picture", HorizontalAlignment = HorizontalAlignment.Center };
        changePic.Click += async (s, args) =>
        {
            string? path = await PickPictureAsync();
            if (path != null)
            {
                newPicture = path;
                SetPicture(pic, path);
            }
        };
        var idBox = new TextBox { Header = "User ID", Text = _userId };
        var error = new TextBlock { Foreground = new SolidColorBrush(Colors.Red) };
        ContentDialog dialog = MakeDialog("Edit profile", new StackPanel { Spacing = 10, Children = { pic, changePic, idBox, error } }, "Save");
        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string id = idBox.Text.Trim();
            if (id.Length == 0)
            {
                error.Text = "User ID cannot be empty.";
                continue;
            }

            if (newPicture != null)
            {
                _picture = ProfileStore.CopyPicture(newPicture);
            }
            _userId = id;
            ShowProfile();
            ProfileStore.Save(_userEmail, _userId, _picture.StartsWith("http") ? "" : _picture);
            return;
        }
    }

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        _inMeeting = false;
        _userId = "";
        _userEmail = "";
        _picture = "";
        _pickedPicture = null;
        UserIdBox.Text = "";
        LoginPic.ProfilePicture = null;
        ProfilePic.ProfilePicture = null;
        ShowScreen("login");
    }

    // dashboard
    private void Start_Click(object sender, RoutedEventArgs e)
    {
        // The real meeting ID and port come from the controller.
        string id = "123-456-789";
        string port = "5000";

        StartMeeting(id, port, true);
    }

    // the person who joins with a meeting ID and port is a Participant
    private async void Join_Click(object sender, RoutedEventArgs e)
    {
        var idBox = new TextBox { Header = "Meeting ID", PlaceholderText = "e.g. 123-456-789" };
        var portBox = new TextBox { Header = "Port", PlaceholderText = "e.g. 5000" };
        var error = new TextBlock { Foreground = new SolidColorBrush(Colors.Red) };
        ContentDialog dialog = MakeDialog("Join meeting", new StackPanel { Spacing = 10, Children = { idBox, portBox, error } }, "Join");

        // keep showing the dialog until the details are fine or the user cancels
        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string id = idBox.Text.Trim();
            string port = portBox.Text.Trim();

            if (id.Length == 0)
            {
                error.Text = "Please enter the meeting ID.";
                continue;
            }

            if (!int.TryParse(port, out int number) || number < 1 || number > 65535)
            {
                error.Text = "Port must be a number between 1 and 65535.";
                continue;
            }

            // Connect to the meeting using this id and port.
            StartMeeting(id, port, false);
            return;
        }
    }

    // inside a meeting
    private void StartMeeting(string id, string port, bool host)
    {
        _inMeeting = true;
        _isHost = host;
        _meetingId = id;
        _meetingPort = port;

        EndBtn.Content = host ? "End Meeting" : "Leave Meeting";
        ShowProfile();

        // CONTROLLER TEAM: call SetParticipants() with the real number
        SetParticipants(1);

        ChatBtn.IsChecked = false;
        ShowChat(false);
        ShowPanel("home");
        ShowScreen("meeting");
    }
    public void SetParticipants(int count)
    {
        MeetingInfoText.Text = "Role: " + Role + "   ID: " + _meetingId + "   Port: " + _meetingPort + "   Participants: " + count;
    }
    private void Panel_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel((string)((FrameworkElement)sender).Tag);
    }

    private void ShowPanel(string which)
    {
        HomePanel.Visibility = Vis(which == "home");
        SharePanel.Visibility = Vis(which == "share");
        BoardPanel.Visibility = Vis(which == "board");
        IncidentsPanel.Visibility = Vis(which == "incidents");
        LocationPanel.Visibility = Vis(which == "location");
        HomeBtn.IsChecked = which == "home";
        ShareBtn.IsChecked = which == "share";
        BoardBtn.IsChecked = which == "board";
        IncidentsBtn.IsChecked = which == "incidents";
        LocationBtn.IsChecked = which == "location";
    }

    private void ChatBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowChat(ChatBtn.IsChecked == true);
    }

    // the chat panel and its drag handle are shown or hidden together
    private void ShowChat(bool show)
    {
        ChatPanel.Visibility = Vis(show);
        ChatResizer.Visibility = Vis(show);
    }

    private async void End_Click(object sender, RoutedEventArgs e)
    {
        string text = _isHost
            ? "Are you sure you want to end this meeting?"
            : "Are you sure you want to leave this meeting?";
        string buttonText = EndBtn.Content.ToString() ?? "";

        ContentDialogResult result = await MakeDialog(buttonText, text, buttonText).ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }
        _records.Insert(0, "Meeting " + _meetingId + " - " + DateTime.Now.ToString("ddd, dd MMM") + " - Just now");
        _inMeeting = false;
        ShowProfile();
        ShowScreen("dash");
    }

    // resizing the chat
    private void ChatResizer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _resizingChat = true;
        ChatResizer.CapturePointer(e.Pointer);
    }

    private void ChatResizer_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizingChat)
        {
            return;
        }
        double cursorX = e.GetCurrentPoint(MeetingView).Position.X;
        double width = MeetingView.ActualWidth - 16 - cursorX;
        ChatPanel.Width = Math.Clamp(width, 250, MeetingView.ActualWidth / 2);
    }

    private void ChatResizer_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _resizingChat = false;
        ChatResizer.ReleasePointerCapture(e.Pointer);
    }
}
public sealed class ResizeHandle : ContentControl
{
    public ResizeHandle()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
