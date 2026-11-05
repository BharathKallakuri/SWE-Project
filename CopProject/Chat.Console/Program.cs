using System.Net;
using System.Net.Sockets;
using Networking.TCPCommunication;

namespace Chat;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            PrintUsage();
            return;
        }

        try
        {
            switch (args[0])
            {
                case "host" when args.Length == 4:
                    RunHost(args[1], args[2], ParsePort(args[3]));
                    break;

                case "client" when args.Length == 4:
                    RunClient(
                        args[1],
                        args[2],
                        ParsePort(args[3]));
                    break;

                default:
                    PrintUsage();
                    Environment.ExitCode = 2;
                    break;
            }
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Environment.ExitCode = 2;
        }
        catch (SocketException exception)
        {
            Console.Error.WriteLine($"Network error: {exception.Message}");
            Environment.ExitCode = 1;
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Environment.ExitCode = 1;
        }
    }

    private static void RunHost(string username, string hostIp, int serverPort)
    {
        using CommunicatorServer communicator = new(new IPEndPoint(IPAddress.Parse(hostIp), serverPort));
        using MockChatController controller = new(username, communicator);
        using ChatServer server = new(
            new Chatroom("console-chat", username),
            communicator,
            controller);
        server.OperationApplied += operation =>
        {
            if (operation is MessageAdded added)
            {
                Console.WriteLine($"{added.Message.SenderName}: {added.Message.Content}");
            }
        };

        RunChatInput(username, content =>
        {
            RoomResult result = server.SubmitHostRequest(new SendMessage(
                DateTimeOffset.UtcNow,
                new Mentions(),
                content,
                null));
            if (result != RoomResult.Ok)
            {
                Console.Error.WriteLine($"Could not send message: {result}");
            }
        });
    }

    private static void RunClient(
        string username,
        string hostIp,
        int serverPort)
    {
        IPEndPoint serverAddress = new(IPAddress.Parse(hostIp), serverPort);
        using CommunicatorClient communicator = new(serverAddress);
        using ChatClient client = new(
            username,
            new Chatroom("console-chat", "host"),
            communicator,
            serverAddress);
        using MockChatClientController controller = new(username, communicator, serverAddress);

        client.OperationApplied += operation =>
        {
            if (operation is MessageAdded added)
            {
                Console.WriteLine($"{added.Message.SenderName}: {added.Message.Content}");
            }
        };

        try
        {
            controller.JoinMeeting();
        }
        catch (SocketException exception)
        {
            Console.Error.WriteLine($"Could not connect to the host: {exception.Message}");
            Environment.ExitCode = 1;
            return;
        }

        RunChatInput(client.Username, content => client.SendMessage(content, new Mentions()));
    }

    private static void RunChatInput(
        string username,
        Action<string> sendMessage)
    {
        Console.WriteLine($"Connected as {username}. Type a message; use /quit to leave.");
        while (Console.ReadLine() is string content)
        {
            if (string.Equals(content, "/quit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            try
            {
                sendMessage(content);
            }
            catch (SocketException exception)
            {
                Console.Error.WriteLine($"Could not send message: {exception.Message}");
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine($"Could not send message: {exception.Message}");
            }
        }
    }

    private static int ParsePort(string value)
    {
        if (!int.TryParse(value, out int port) || port is < 1 or > 65535)
        {
            throw new ArgumentException($"'{value}' is not a valid TCP port.");
        }

        return port;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Chat host <username> <bind-ip> <server-port>");
        Console.WriteLine("  Chat client <username> <host-ip> <server-port>");
        Console.WriteLine();
        Console.WriteLine("Use 127.0.0.1 for two terminals on one computer.");
        Console.WriteLine("For another computer, use the host's reachable IP address.");
    }
}
