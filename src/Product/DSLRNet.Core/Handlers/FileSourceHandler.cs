namespace DSLRNet.Core.Handlers;

using System.Diagnostics.CodeAnalysis;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

public class FileSourceHandler(IOptions<Settings> settings)
{
    public void ParseModDirectoriesFromToml(string fileName)
    {
        string fullPath;
        if (!TryGetFile(fileName, out fullPath))
        {
            throw new FileNotFoundException($"File {fileName} not found in any of the specified paths.");
        }

        var tomlContent = File.ReadAllText(fullPath);
        var table = Toml.Parse(tomlContent).ToModel();

        var modPaths = new List<string>();

        if (table.TryGetValue("extension.mod_loader", out var modLoaderSection))
        {
            var modLoaderTable = modLoaderSection as TomlTable;
            if (modLoaderTable != null && modLoaderTable.TryGetValue("mods", out var mods))
            {
                var modsArray = mods as TomlArray;
                if (modsArray != null)
                {
                    foreach (var mod in modsArray.OfType<TomlTable>())
                    {
                        if (mod.TryGetValue("enabled", out var enabled)
                            && (bool)enabled
                            && mod.TryGetValue("path", out var path))
                        {
                            modPaths.Add(Path.Combine(Path.GetDirectoryName(fileName), path.ToString()));
                        }
                    }
                }
            }
        }

        settings.Value.OrderedModPaths = modPaths;
    }

    public bool TryGetFile(string fileName, [NotNullWhen(true)] out string fullPath)
    {
        if (settings.Value.OrderedModPaths.Count == 0)
        {
            throw new InvalidOperationException("Mod paths not set");
        }

        fullPath = null;

        foreach (string path in settings.Value.OrderedModPaths)
        {
            var combined = Path.Combine(path, fileName);
            if (File.Exists(combined))
            {
                fullPath = combined;
                return true;
            }
        }

        if (File.Exists(Path.Combine(settings.Value.GamePath, fileName)))
        {
            fullPath = Path.Combine(settings.Value.GamePath, fileName);
            return true;
        }

        return false;
    }

    public List<string> ListFilesFromAllModDirectories(string basePath, string filter)
    {
        if (settings.Value.OrderedModPaths.Count == 0)
        {
            throw new InvalidOperationException("Mod paths not set");
        }

        var files = new List<string>();
        foreach (string path in settings.Value.OrderedModPaths)
        {
            var testPath = Path.Combine(path, basePath);
            if (Directory.Exists(testPath))
            {
                files.AddRange(
                    Directory.GetFiles(testPath, filter)
                        .Where(d => !files.Any(t => Path.GetFileName(t) == Path.GetFileName(d)))
                        .ToList());
            }
        }

        // pick up any files in the game path that are not in the mod paths, just in case we need them
        files.AddRange(
            Directory.GetFiles(Path.Combine(settings.Value.GamePath, basePath), filter)
                .Where(d => !files.Any(t => Path.GetFileName(t) == Path.GetFileName(d)))
                .ToList());

        return files;
    }
}
