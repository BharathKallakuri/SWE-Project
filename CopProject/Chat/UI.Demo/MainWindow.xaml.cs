using System.Net;
using Microsoft.UI.Xaml;
using UI.Demo.Services;

namespace UI.Demo;

public sealed partial class MainWindow : Window
{
    private readonly DemoParticipantSession _session;
    private readonly Action? _openAnotherParticipant;

    public MainWindow(
        DemoParticipantSession session,
        Action? openAnotherParticipant,
        IPEndPoint serverEndpoint)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(serverEndpoint);

        InitializeComponent();

        _session = session;
        _openAnotherParticipant = openAnotherParticipant;

        Title = $"Chat UI Demo - {_session.Client.Username}";
        ServerAddressText.Text = $"Demo server: {serverEndpoint.Address}:{serverEndpoint.Port}";

        // Supply the participant-specific ViewModel to the reusable UserControl.
        ChatPanelControl.ViewModel = _session.ViewModel;
        _session.ViewModel.Initialize();
        OpenParticipantButton.Visibility = openAnotherParticipant is null
            ? Visibility.Collapsed
            : Visibility.Visible;

        // This window owns the participant session and disposes it on close.
        Closed += (_, _) => _session.Dispose();
    }

    private void OpenParticipant_Click(object sender, RoutedEventArgs e)
    {
        _openAnotherParticipant?.Invoke();
    }
}
