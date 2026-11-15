// Author = Vishesh Srivastava
// Roll Number = 112301037

using System.Xml.Linq;

DirectoryInfo? currentDirectory = new(Directory.GetCurrentDirectory());
DirectoryInfo? rootDirectory = (currentDirectory.Parent?.Parent) ?? throw new DirectoryNotFoundException("Could not find the repository root.");

string root = rootDirectory.FullName;

/// <summary>
/// Find all .csproj files in the repository.
/// </summary>
string[] allFiles = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories);

List<string> projects = new List<string>();
foreach (string file in allFiles)
{
    if (file.Contains("/obj/") || file.Contains("\\obj\\"))
    {
        continue;
    }
    projects.Add(file);
}

Console.WriteLine("graph LR");

foreach (string project in projects)
{
    string name = Path.GetFileNameWithoutExtension(project);
    string id = name.Replace(".", "_");

    string style = "";
    if (name.Contains("Test", StringComparison.OrdinalIgnoreCase))
    {
        style = ":::test";
    }
    else if (name.Contains("Harness", StringComparison.OrdinalIgnoreCase))
    {
        style = ":::harness";
    }

    Console.WriteLine($"  {id}[\"{name}\"]{style}");
}
/// <summary>   
/// Step 3: print one arrow per ProjectReference
/// "A --> B" means "A depends on B"
/// </summary>
foreach (string project in projects)
{
    string fromName = Path.GetFileNameWithoutExtension(project);
    string fromId = fromName.Replace(".", "_");

    XDocument doc = XDocument.Load(project);

    foreach (XElement reference in doc.Descendants("ProjectReference"))
    {
        XAttribute? include = reference.Attribute("Include");
        if (include == null)
        {
            continue;
        }

        string path = include.Value.Replace("\\", "/");
        string toName = Path.GetFileNameWithoutExtension(path);
        string toId = toName.Replace(".", "_");

        Console.WriteLine($"  {fromId} --> {toId}");
    }
}

Console.WriteLine("  classDef test fill:#fff3cd,stroke:#856404,color:#000");
Console.WriteLine("  classDef harness fill:#dbeafe,stroke:#1d4ed8,color:#000");
