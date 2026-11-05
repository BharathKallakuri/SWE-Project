using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UX;

public sealed partial class MainWindow : Window
{
    // list shown in "Previous Meeting Records"
    ObservableCollection<string> records = new ObservableCollection<string>()
    {
        "Weekly Review - Mon, 28 Sep - 45 min",
        "Patrol Planning - Fri, 25 Sep - 30 min",
        "Case Briefing - Wed, 23 Sep - 1 hr 10 min"
    };

    // only college emails can use the app
    const string AllowedDomain = "@smail.iitpkd.ac.in";

    string userId = "";
    string meetingId = "";
    string meetingPort = "";
    bool inMeeting = false;
    bool isHost = false;   // host starts the meeting, client joins it
    bool resizingChat = false;

    public MainWindow()
    {
        InitializeComponent();
        Title = "SecureMeet";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        RecordsList.ItemsSource = records;
        ShowScreen("login");
    }

    Visibility Vis(bool show) { return show ? Visibility.Visible : Visibility.Collapsed; }
    void ShowScreen(string screen)
    {
        LoginView.Visibility = Vis(screen == "login");
        DashView.Visibility = Vis(screen == "dash");
        MeetingView.Visibility = Vis(screen == "meeting");
        IncidentsView.Visibility = Vis(screen == "incidents");
        UserPanel.Visibility = Vis(screen != "login");
        MeetingInfoText.Visibility = Vis(inMeeting);
    }

    ContentDialog MakeDialog(string title, object content, string okText)
    {
        return new ContentDialog { Title = title, Content = content, PrimaryButtonText = okText, CloseButtonText = "Cancel", XamlRoot = Root.XamlRoot };
    }

    // login

    void Continue_Click(object sender, RoutedEventArgs e)
    {
        string id = UserIdBox.Text.Trim();
        string mail = EmailBox.Text.Trim().ToLower();

        if (id == "")
        {
            LoginError.Text = "Please enter your user ID.";
            LoginError.Visibility = Visibility.Visible;
            return;
        }
        if (!mail.EndsWith(AllowedDomain) || mail == AllowedDomain)
        {
            LoginError.Text = "Please use your " + AllowedDomain + " email address.";
            LoginError.Visibility = Visibility.Visible;
            return;
        }

        userId = id;
        LoginError.Visibility = Visibility.Collapsed;
        ProfileName.Text = userId;
        ProfilePic.DisplayName = userId;

        int hour = DateTime.Now.Hour;
        string greeting = "Good Evening";
        if (hour < 12) greeting = "Good Morning";
        else if (hour < 17) greeting = "Good Afternoon";
        GreetingText.Text = greeting + ", " + userId + "!";

        ShowScreen("dash");
    }

    void SignOut_Click(object sender, RoutedEventArgs e)
    {
        inMeeting = false;
        ShowScreen("login");
    }

    // dashboard

    // the person who starts the meeting is the host
    void Start_Click(object sender, RoutedEventArgs e)
    {
        //the real meeting ID and port come from the controller.
        string id = "123-456-789";
        string port = "5000";

        StartMeeting(id, port, true);
    }

    // the person who joins with a meeting ID and port is the client
    async void Join_Click(object sender, RoutedEventArgs e)
    {
        var idBox = new TextBox { Header = "Meeting ID", PlaceholderText = "e.g. 123-456-789" };
        var portBox = new TextBox { Header = "Port", PlaceholderText = "e.g. 5000" };
        var error = new TextBlock { Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) };
        var dialog = MakeDialog("Join meeting", new StackPanel { Spacing = 10, Children = { idBox, portBox, error } }, "Join");
        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string id = idBox.Text.Trim();
            string port = portBox.Text.Trim();

            if (id == "") { error.Text = "Please enter the meeting ID."; continue; }
            if (!int.TryParse(port, out int number) || number < 1 || number > 65535)
            {
                error.Text = "Port must be a number between 1 and 65535.";
                continue;
            }

            // CONTROLLER TEAM: connect to the meeting using this id and port.
            StartMeeting(id, port, false);
            return;
        }
    }

    // inside meeting 

    void StartMeeting(string id, string port, bool host)
    {
        inMeeting = true;
        isHost = host;
        meetingId = id;
        meetingPort = port;

        EndBtn.Content = host ? "End Meeting" : "Leave Meeting";

        // CONTROLLER TEAM: call SetParticipants() with the real number
        SetParticipants(1);

        ChatBtn.IsChecked = false;
        ShowChat(false);
        ShowPanel("home");
        ShowScreen("meeting");
    }
    public void SetParticipants(int count)
    {
        MeetingInfoText.Text = "ID: " + meetingId + "   Port: " + meetingPort + "   Participants: " + count;
    }

    // Home, Share and Whiteboard use the same area
    void ShowPanel(string which)
    {
        HomePanel.Visibility = Vis(which == "home");
        SharePanel.Visibility = Vis(which == "share");
        BoardPanel.Visibility = Vis(which == "board");
        HomeBtn.IsChecked = which == "home";
        ShareBtn.IsChecked = which == "share";
        BoardBtn.IsChecked = which == "board";
    }

    void HomeBtn_Click(object sender, RoutedEventArgs e) { ShowPanel("home"); }
    void ShareBtn_Click(object sender, RoutedEventArgs e) { ShowPanel("share"); }
    void BoardBtn_Click(object sender, RoutedEventArgs e) { ShowPanel("board"); }
    void ChatBtn_Click(object sender, RoutedEventArgs e) { ShowChat(ChatBtn.IsChecked == true); }
    void ShowChat(bool show)
    {
        ChatPanel.Visibility = Vis(show);
        ChatResizer.Visibility = Vis(show);
    }

    async void End_Click(object sender, RoutedEventArgs e)
    {
        string text = isHost ? "Are you sure you want to end this meeting?" : "Are you sure you want to leave this meeting?";
        var result = await MakeDialog(EndBtn.Content.ToString(), text, EndBtn.Content.ToString()).ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        // CONTROLLER TEAM: end the meeting (host) or disconnect (client) here.
        records.Insert(0, "Meeting " + meetingId + " - " + DateTime.Now.ToString("ddd, dd MMM") + " - Just now");
        inMeeting = false;
        ShowScreen("dash");
    }

    //resizing the chat

    void ChatResizer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        resizingChat = true;
        ChatResizer.CapturePointer(e.Pointer);
    }

    void ChatResizer_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!resizingChat) return;
        double cursorX = e.GetCurrentPoint(MeetingView).Position.X;
        double width = MeetingView.ActualWidth - 16 - cursorX;
        ChatPanel.Width = Math.Clamp(width, 250, MeetingView.ActualWidth / 2);
    }

    void ChatResizer_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        resizingChat = false;
        ChatResizer.ReleasePointerCapture(e.Pointer);
    }

    // incidents
    void Incidents_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen("incidents");
    }

    // Back goes to the meeting if we are in one, otherwise to the dashboard
    void IncidentsBack_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen(inMeeting ? "meeting" : "dash");
    }
}
public sealed class ResizeHandle : ContentControl
{
    public ResizeHandle()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
