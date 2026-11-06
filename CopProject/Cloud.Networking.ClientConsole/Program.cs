// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;

public class Program
{
    private static async Task Main(string[] args)
    {
        // Do: dotnet run -- http://localhost:5000/hub
        string url = args.Length > 0 ? args[0] : "http://localhost:5000/hub";

        await using SignalRMessenger messenger = new(url);

        // register all the subscribers
        messenger.MessageReceived += m => Console.WriteLine($"[{m.Timestamp.ToLocalTime():HH:mm:ss}] {m.SenderId}: {m.Content}");
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

        Console.WriteLine($"Connected to {url}. Type a non-blank message and press Enter. Type /c to connect again, /quit or ^C to exit.");

        while (Console.ReadLine() is { } line)
        {
            if (line == "/quit")
            {
                break;
            }

            if (line == "/c")
            {
                try
                {
                    await messenger.ConnectAsync();
                    Console.WriteLine("-- connected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"-- connection failed: {ex.Message}");
                }

                continue;
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

        // just disconnect not dispose
        await messenger.DisconnectAsync();
    }
}
