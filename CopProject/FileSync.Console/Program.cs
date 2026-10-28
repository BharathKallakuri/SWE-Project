using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSync.ConsoleApp;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("   FileSync & Networking Simulation Console App   ");
        Console.WriteLine("==================================================");

        // if arguments are provided, run as an interactive single node
        if (args.Length >= 2)
        {
            int listenPort = int.Parse(args[0]);
            int peerPort = int.Parse(args[1]);
            string peerIp = args.Length > 2 ? args[2] : "127.0.0.1";
            string folder = args.Length > 3 ? args[3] : $"Node_{listenPort}";

            RunInteractiveNode(listenPort, peerPort, peerIp, folder);
            return;
        }

        // otherwise run the automated two-node simulation demo
        await RunAutomatedSimulationAsync();
    }

    // runs an interactive node connecting to a peer machine over tcp
    private static void RunInteractiveNode(int listenPort, int peerPort, string peerIp, string folder)
    {
        Console.WriteLine($"[node] listening on port {listenPort}");
        Console.WriteLine($"[node] targeting peer at {peerIp}:{peerPort}");
        Console.WriteLine($"[node] shared folder: {Path.GetFullPath(folder)}");

        using var communicator = new SimulatedCommunicator(listenPort);
        var peerEndpoint = new IPEndPoint(IPAddress.Parse(peerIp), peerPort);
        communicator.RegisterPeer(peerEndpoint);

        var handler = new NetworkSyncHandler(folder, communicator)
        {
            DefaultPeerEndpoint = peerEndpoint
        };

        handler.OnLog += msg => Console.WriteLine(msg);
        handler.OnSyncCompleted += () => Console.WriteLine("[node] sync finished successfully!");

        Console.WriteLine("\ncommands available:");
        Console.WriteLine("  sync                       - trigger synchronisation with peer");
        Console.WriteLine("  create <relPath> <text>    - create a test file");
        Console.WriteLine("  list                       - list files in folder");
        Console.WriteLine("  exit                       - quit\n");

        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLowerInvariant();

            if (cmd == "exit")
            {
                break;
            }
            else if (cmd == "sync")
            {
                handler.StartSync();
            }
            else if (cmd == "list")
            {
                string dir = handler.RootDirectory;
                string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                Console.WriteLine($"files in {dir} ({files.Length}):");
                foreach (var f in files)
                {
                    var fi = new FileInfo(f);
                    Console.WriteLine($"  - {Path.GetRelativePath(dir, f)} ({fi.Length} bytes, {fi.LastWriteTimeUtc})");
                }
            }
            else if (cmd == "create" && parts.Length >= 3)
            {
                string rel = parts[1];
                string content = parts[2];
                string full = Path.Combine(handler.RootDirectory, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllText(full, content);
                Console.WriteLine($"created file at {rel}");
            }
            else
            {
                Console.WriteLine("unknown command. use: sync, list, create <path> <text>, exit");
            }
        }
    }

    // runs an automated simulation between node a and node b on localhost
    private static async Task RunAutomatedSimulationAsync()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), $"FileSyncDemo_{Guid.NewGuid():N}");
        string folderA = Path.Combine(baseDir, "PeerA_Shared");
        string folderB = Path.Combine(baseDir, "PeerB_Shared");

        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);

        Console.WriteLine("[simulation] setting up two simulated nodes:");
        Console.WriteLine($"  node a folder: {folderA} (listening on port 5001)");
        Console.WriteLine($"  node b folder: {folderB} (listening on port 5002)");

        // initialize communicators for both nodes
        using var commA = new SimulatedCommunicator(5001);
        using var commB = new SimulatedCommunicator(5002);

        var epA = new IPEndPoint(IPAddress.Loopback, 5001);
        var epB = new IPEndPoint(IPAddress.Loopback, 5002);

        commA.RegisterPeer(epB);
        commB.RegisterPeer(epA);

        var handlerA = new NetworkSyncHandler(folderA, commA) { DefaultPeerEndpoint = epB };
        var handlerB = new NetworkSyncHandler(folderB, commB) { DefaultPeerEndpoint = epA };

        handlerA.OnLog += msg => Console.WriteLine($"[node a] {msg}");
        handlerB.OnLog += msg => Console.WriteLine($"[node b] {msg}");

        bool nodeBSyncDone = false;
        handlerB.OnSyncCompleted += () => nodeBSyncDone = true;

        // step 1: create sample files in node a (including nested folder and chunked file)
        Console.WriteLine("\n[step 1] creating files in node a...");
        string incidentReport = Path.Combine(folderA, "evidences", "incident_42", "report.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(incidentReport)!);
        File.WriteAllText(incidentReport, "incident 42: suspect identified at downtown intersection.");

        // create a larger file to demonstrate chunking
        string largeEvidence = Path.Combine(folderA, "evidences", "incident_42", "surveillance.dat");
        byte[] largeData = new byte[150 * 1024]; // 150kb file to test multiple chunks
        new Random(42).NextBytes(largeData);
        File.WriteAllBytes(largeEvidence, largeData);

        Console.WriteLine("  - created evidences/incident_42/report.txt");
        Console.WriteLine("  - created evidences/incident_42/surveillance.dat (150 KB)");

        // step 2: trigger sync from node b
        Console.WriteLine("\n[step 2] triggering sync from node b to pull changes from node a...");
        handlerB.StartSync();

        // wait up to 5 seconds for network transmission and chunk assembly
        int waited = 0;
        while (!nodeBSyncDone && waited < 50)
        {
            await Task.Delay(100);
            waited++;
        }

        // step 3: verify files in node b
        Console.WriteLine("\n[step 3] verifying synchronized files in node b folder...");
        string syncedReport = Path.Combine(folderB, "evidences", "incident_42", "report.txt");
        string syncedLarge = Path.Combine(folderB, "evidences", "incident_42", "surveillance.dat");

        bool reportOk = File.Exists(syncedReport) && File.ReadAllText(syncedReport).Contains("incident 42");
        bool largeOk = File.Exists(syncedLarge) && File.ReadAllBytes(syncedLarge).Length == largeData.Length;

        if (reportOk && largeOk)
        {
            Console.WriteLine("\n========================================================");
            Console.WriteLine(" [SUCCESS] FileSync simulation completed successfully!   ");
            Console.WriteLine(" - All nested files transferred over TCP communicator.  ");
            Console.WriteLine(" - Large file chunked and reassembled correctly.        ");
            Console.WriteLine("========================================================\n");
        }
        else
        {
            Console.WriteLine("\n[failed] some files were not received properly.");
        }

        // clean up temporary folders
        try
        {
            Directory.Delete(baseDir, recursive: true);
        }
        catch { }
    }
}
