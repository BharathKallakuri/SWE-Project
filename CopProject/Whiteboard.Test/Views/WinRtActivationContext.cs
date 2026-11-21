/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace Whiteboard.Test.Views;

/// <summary>
/// Builds a side-by-side activation manifest from the deployed WinAppSDK fragments
/// and activates it on the calling thread so unpackaged WinRT/WinUI classes can be created.
/// </summary>
internal static class WinRtActivationContext
{
    private const uint ActctxWithWindowsVersion = 0;
    private static readonly XNamespace s_fragmentNamespace = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
    private static readonly object s_gate = new();
    [ThreadStatic]
    private static bool s_threadActivated;
    private static IntPtr s_handle = new(-1);

    public static void EnsureActivated()
    {
        if (s_threadActivated)
        {
            return;
        }

        if (s_handle == new IntPtr(-1))
        {
            lock (s_gate)
            {
                if (s_handle == new IntPtr(-1))
                {
                    string manifestPath = BuildManifest();
                    ACTCTX context = new() {
                        _cbSize = Marshal.SizeOf<ACTCTX>(),
                        _dwFlags = ActctxWithWindowsVersion,
                        _lpSource = manifestPath
                    };

                    IntPtr handle = CreateActCtxW(ref context);
                    if (handle == new IntPtr(-1))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(),
                            "Could not create the Windows App SDK activation context.");
                    }

                    s_handle = handle;
                }
            }
        }

        if (!ActivateActCtx(s_handle, out _))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Could not activate the Windows App SDK activation context.");
        }

        s_threadActivated = true;
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
            foreach (XElement server in document.Descendants(s_fragmentNamespace + "InProcessServer"))
            {
                string path = (string?)server.Element(s_fragmentNamespace + "Path") ?? string.Empty;
                if (path.Length == 0)
                {
                    continue;
                }

                builder.Append("    <asmv3:file name='").Append(path).Append("'>\n");
                foreach (XElement activatableClass in server.Elements(s_fragmentNamespace + "ActivatableClass"))
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

        string manifestPath = Path.Combine(AppContext.BaseDirectory, "WindowsAppSDK.activatableclasses.manifest");
        File.WriteAllText(manifestPath, builder.ToString());
        return manifestPath;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ACTCTX
    {
        public int _cbSize;
        public uint _dwFlags;
        public string? _lpSource;
        public ushort _wProcessorArchitecture;
        public ushort _wLangId;
        public string? _lpAssemblyDirectory;
        public string? _lpResourceName;
        public string? _lpApplicationName;
        public IntPtr _hModule;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateActCtxW(ref ACTCTX actCtx);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ActivateActCtx(IntPtr hActCtx, out ulong lpCookie);
}
