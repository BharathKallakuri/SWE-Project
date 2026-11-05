using System.Net;
using Microsoft.UI.Xaml;

namespace UI.Demo;

public sealed partial class ConnectionWindow : Window
{
    public event Action<string, IPAddress, int>? HostRequested;
    public event Action<string, IPEndPoint>? JoinRequested;

    public ConnectionWindow()
    {
        InitializeComponent();
    }

    private void Host_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadUsername(out string username)
            || !TryReadPort(out int port)
            || !TryReadAddress(BindAddressTextBox.Text, "bind IP", out IPAddress bindAddress))
        {
            return;
        }

        HostRequested?.Invoke(username, bindAddress, port);
    }

    private void Join_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadUsername(out string username)
            || !TryReadPort(out int port)
            || !TryReadAddress(HostAddressTextBox.Text, "host IP", out IPAddress hostAddress))
        {
            return;
        }

        JoinRequested?.Invoke(username, new IPEndPoint(hostAddress, port));
    }

    private bool TryReadUsername(out string username)
    {
        username = UsernameTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(username))
            return true;

        ShowError("Enter a username.");
        return false;
    }

    private bool TryReadPort(out int port)
    {
        if (int.TryParse(PortTextBox.Text, out port) && port is >= 1 and <= 65535)
            return true;

        ShowError("Enter a TCP port from 1 to 65535.");
        return false;
    }

    private bool TryReadAddress(
        string? value,
        string fieldName,
        out IPAddress address)
    {
        if (IPAddress.TryParse(value, out IPAddress? parsedAddress)
            && parsedAddress is not null)
        {
            address = parsedAddress;
            return true;
        }

        address = IPAddress.None;
        ShowError($"Enter a valid {fieldName}.");
        return false;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
