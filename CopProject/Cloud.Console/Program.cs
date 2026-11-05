// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        // Do: dotnet run -- http://localhost:5000/hub
        string url = args.Length > 0 ? args[0] : "http://localhost:5000/hub";

        await using SignalRMessenger messenger = new(url);

        messenger.MessageReceived += m =>Console.WriteLine($"[{m.Timestamp.ToLocalTime():HH:mm:ss}] {m.SenderId}: {m.Content}");
        messenger.Reconnecting += _ => Console.WriteLine("-- connection lost, reconnecting...");
        messenger.Reconnected += _ => Console.WriteLine("-- reconnected");
        messenger.Closed += e => Console.WriteLine(e is null ? "-- disconnected" : $"-- connection closed: {e.Message}");

        try
        {
            await messenger.ConnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not connect to {url}: {ex.Message}");
            return;
        }

        Console.WriteLine($"Connected to {url}. Type a message and press Enter. Type /quit to exit.");

        while (Console.ReadLine() is { } line)
        {
            if (line == "/quit")
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                await messenger.SendMessageAsync(line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-- send failed: {ex.Message}");
            }
        }

        await messenger.DisconnectAsync();
    }
}
