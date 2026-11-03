using System.Diagnostics;
using Controller;

// ── Thin test harness — no business logic here ───────────────────────
Trace.Listeners.Add(new ConsoleTraceListener());

Console.WriteLine("=== Controller-Networking Test Harness ===");
Console.WriteLine("Choose role: [H]ost or [C]lient?");

string? role = Console.ReadLine()?.Trim().ToUpperInvariant();

if (role == "H")
{
    RunHost();
}
else if (role == "C")
{
    RunClient();
}
else
{
    Console.WriteLine("Invalid choice. Exiting.");
}

// ── Host flow ────────────────────────────────────────────────────────
void RunHost()
{
    Console.Write("Enter your host user ID: ");
    string hostName = Console.ReadLine()?.Trim() ?? "host";
    var hostUser = new ControllerUser(hostName, $"{hostName}@example.com", null, string.Empty, 0, isHost: true);

    using IControllerService host = ControllerFactory.CreateHost(hostUser);

    // Subscribe to the 3 unified events
    host.UserJoined += (_, e) => Console.WriteLine($"  + [{(e.User.IsHost ? "Host" : "Participant")}] {e.User.Username} joined (total: {e.NewCount})");
    host.UserLeft += (_, e) => Console.WriteLine($"  - {e.User.Username} left (total: {e.NewCount})");
    host.MeetingEnded += (_, e) => Console.WriteLine($"  ■ Meeting ended by {e.Host.Username}");

    Console.WriteLine($"Listening on port {host.ListenPort}");
    Console.WriteLine("Commands: start | end | count | users | quit");

    while (true)
    {
        Console.Write("> ");
        string? input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input)) continue;

        string command = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();

        switch (command)
        {
            case "start":
                bool started = host.StartMeeting();
                Console.WriteLine(started ? "Meeting started." : "Failed to start meeting.");
                break;

            case "end":
                if (host is HostController hcEnd && !hcEnd.IsMeetingActive)
                {
                    Console.WriteLine("You're not in a meeting.");
                    break;
                }
                bool ended = host.EndMeeting();
                Console.WriteLine(ended ? "Meeting ended." : "Failed to end meeting.");
                break;

            case "count":
                if (host is HostController hcCount && !hcCount.IsMeetingActive)
                {
                    Console.WriteLine("You're not in a meeting.");
                }
                else
                {
                    Console.WriteLine($"{host.GetActiveUserCount()} user(s) online");
                }
                break;

            case "users":
                if (host is HostController hcUsers && !hcUsers.IsMeetingActive)
                {
                    Console.WriteLine("You're not in a meeting.");
                }
                else
                {
                    List<ControllerUser> users = host.GetActiveUsers();
                    Console.WriteLine($"{users.Count} user(s): {string.Join(", ", users.Select(u => $"[{(u.IsHost ? "Host" : "Participant")}] {u.Username} ({u.Gmail}) @ {u.Ip}:{u.Port}"))}");
                }
                break;

            case "quit":
                return;

            default:
                Console.WriteLine("Unknown command. Try: start, end, count, users, quit");
                break;
        }
    }
}

// ── Client flow ──────────────────────────────────────────────────────
void RunClient()
{
    Console.Write("Enter your user ID: ");
    string userName = Console.ReadLine()?.Trim() ?? "user";
    var clientUser = new ControllerUser(userName, $"{userName}@example.com", null, string.Empty, 0, isHost: false);

    using IControllerService client = ControllerFactory.CreateClient(clientUser);

    // Subscribe to the 3 unified events
    client.UserJoined += (_, e) => Console.WriteLine($"  + [{(e.User.IsHost ? "Host" : "Participant")}] {e.User.Username} joined (total: {e.NewCount})");
    client.UserLeft += (_, e) => Console.WriteLine($"  - {e.User.Username} left (total: {e.NewCount})");
    client.MeetingEnded += (_, e) => Console.WriteLine($"  ■ Meeting ended by {e.Host.Username}");

    Console.WriteLine($"Client initialized for user '{clientUser.Username}'.");
    Console.WriteLine("Commands: join <hostIp> <hostPort> | leave | users | count | quit");

    while (true)
    {
        Console.Write("> ");
        string? input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input)) continue;

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string command = parts[0].ToLowerInvariant();

        switch (command)
        {
            case "join" when parts.Length >= 3:
                client.JoinMeeting(parts[1], int.Parse(parts[2]), clientUser);
                Console.WriteLine($"Connected to {parts[1]}:{parts[2]} (local port: {client.ListenPort}).");
                break;

            case "leave":
                if (client is ClientController ccLeave && !ccLeave.IsConnected)
                {
                    Console.WriteLine("You're not in a meeting.");
                    break;
                }
                client.LeaveMeeting(clientUser);
                Console.WriteLine("Left the meeting.");
                break;

            case "users":
                if (client is ClientController ccUsersList && !ccUsersList.IsConnected)
                {
                    Console.WriteLine("You're not in a meeting.");
                }
                else
                {
                    List<ControllerUser> users = client.GetActiveUsers();
                    Console.WriteLine($"{users.Count} user(s): {string.Join(", ", users.Select(u => $"[{(u.IsHost ? "Host" : "Participant")}] {u.Username} ({u.Gmail}) @ {u.Ip}:{u.Port}"))}");
                }
                break;

            case "count":
                if (client is ClientController ccCount && !ccCount.IsConnected)
                {
                    Console.WriteLine("You're not in a meeting.");
                }
                else
                {
                    Console.WriteLine($"{client.GetActiveUserCount()} user(s) online");
                }
                break;

            case "quit":
                return;

            default:
                Console.WriteLine("Usage: join <hostIp> <hostPort> | leave | users | count | quit");
                break;
        }
    }
}