namespace DSLRNet.Core.Handlers;

using System.Diagnostics.CodeAnalysis;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

public class FileSourceHandler(IOptions<Settings> settings)
{
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
