// Author: Rajat
// Roll no.: 112301044

using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.UI.Xaml.Hosting;

namespace Chat.UI.Test;

/// <summary>
/// Unpackaged WinUI apps activate their COM classes (e.g. DispatcherQueueController)
/// through a side-by-side activation manifest instead of the registry. This mirrors that:
/// it builds a manifest from the deployed WinAppSDK fragments and pushes it as the
/// calling thread's activation context, so COM creation succeeds.
/// </summary>
internal static class WinRtActivationContext
{
    private const uint ActctxWithWindowsVersion = 0;
    private static readonly XNamespace FragmentNamespace = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
    private static readonly object Gate = new();
    [ThreadStatic]
    private static bool _threadActivated;
    private static IntPtr _handle = new(-1);

    /// <summary>Creates the activation context once and pushes it onto the calling thread.</summary>
    public static void EnsureActivated()
    {
        if (_threadActivated)
        {
            return;
        }

        if (_handle == new IntPtr(-1))
        {
            lock (Gate)
            {
                if (_handle == new IntPtr(-1))
                {
                    string manifestPath = BuildManifest();
                    ACTCTX context = new()
                    {
                        cbSize = Marshal.SizeOf<ACTCTX>(),
                        dwFlags = ActctxWithWindowsVersion,
                        lpSource = manifestPath
                    };

                    IntPtr handle = CreateActCtxW(ref context);
                    if (handle == new IntPtr(-1))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(),
                            "Could not create the Windows App SDK activation context.");
                    }

                    _handle = handle;
                }
            }
        }

        if (!ActivateActCtx(_handle, out _))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Could not activate the Windows App SDK activation context.");
        }

        _threadActivated = true;
    }

    private static string BuildManifest()
    {
        IEnumerable<string> fragments = typeof(WinRtActivationContext).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Key == "WinAppSdkFragment")
            .Select(attribute => attribute.Value!);

        StringBuilder builder = new(256 * 1024);
        builder.Append("<?xml version='1.0' encoding='utf-8' standalone='yes'?>\n");
        builder.Append("<assembly manifestVersion='1.0'\n");
        builder.Append("    xmlns:asmv3='urn:schemas-microsoft-com:asm.v3'\n");
        builder.Append("    xmlns:winrtv1='urn:schemas-microsoft-com:winrt.v1'\n");
        builder.Append("    xmlns='urn:schemas-microsoft-com:asm.v1'>\n");

        foreach (string fragment in fragments)
        {
            XDocument document = XDocument.Load(fragment);
            foreach (XElement server in document.Descendants(FragmentNamespace + "InProcessServer"))
            {
                string path = (string?)server.Element(FragmentNamespace + "Path") ?? string.Empty;
                if (path.Length == 0)
                {
                    continue;
                }

                builder.Append("    <asmv3:file name='").Append(path).Append("'>\n");
                foreach (XElement activatableClass in server.Elements(FragmentNamespace + "ActivatableClass"))
                {
                    string classId = (string?)activatableClass.Attribute("ActivatableClassId") ?? string.Empty;
                    string threadingModel = ((string?)activatableClass.Attribute("ThreadingModel"))?.ToLowerInvariant() ?? "both";
                    if (classId.Length == 0)
                    {
                        continue;
                    }

                    builder.Append("        <winrtv1:activatableClass name='")
                        .Append(classId)
                        .Append("' threadingModel='")
                        .Append(threadingModel)
                        .Append("'/>\n");
                }

                builder.Append("    </asmv3:file>\n");
            }
        }

        builder.Append("</assembly>\n");

        // The SxS prober resolves the deployed DLLs relative to the manifest's directory,
        // so it has to live next to the test output.
        string manifestPath = Path.Combine(AppContext.BaseDirectory, "WindowsAppSDK.activatableclasses.manifest");
        File.WriteAllText(manifestPath, builder.ToString());
        return manifestPath;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ACTCTX
    {
        public int cbSize;
        public uint dwFlags;
        public string? lpSource;
        public ushort wProcessorArchitecture;
        public ushort wLangId;
        public string? lpAssemblyDirectory;
        public string? lpResourceName;
        public string? lpApplicationName;
        public IntPtr hModule;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateActCtxW(ref ACTCTX actCtx);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ActivateActCtx(IntPtr hActCtx, out ulong lpCookie);
}
