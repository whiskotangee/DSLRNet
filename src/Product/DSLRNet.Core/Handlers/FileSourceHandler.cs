﻿namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.IO;

public class FileSourceHandler(IOptions<Settings> settings)
{
    public bool TryGetFile(string fileName, [NotNullWhen(true)] out string fullPath)
    {
        fullPath = string.Empty;

        foreach (string path in settings.Value.OrderedModPaths)
        {
            string combined = Path.Combine(path, fileName);
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

        if (File.Exists(PathHelper.FullyQualifyAppDomainPath("Assets", "VanillaFiles", fileName)))
        {
            fullPath = PathHelper.FullyQualifyAppDomainPath("Assets", "VanillaFiles", fileName);
            return true;
        }

        return false;
    }

    public List<string> ListFilesFromAllModDirectories(string basePath, string filter, bool recursive = false)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        List<string> files = [];
        foreach (string path in settings.Value.OrderedModPaths.Where(d => !string.Equals(d, settings.Value.DeployPath, StringComparison.OrdinalIgnoreCase)))
        {
            string testPath = Path.Combine(path, basePath);
            if (Directory.Exists(testPath))
            {
                files.AddRange(
                    Directory.GetFiles(testPath, filter, searchOption)
                        .Where(d => !files.Any(t => Path.GetFileName(t) == Path.GetFileName(d)))
                        .ToList());
            }
        }

        // pick up any files in the game path that are not in the mod paths or vanilla files path just in case we need them
        if (Directory.Exists(Path.Combine(settings.Value.GamePath, basePath)))
        {
            files.AddRange(
                Directory.GetFiles(Path.Combine(settings.Value.GamePath, basePath), filter, searchOption)
                    .Where(d => !files.Any(t => Path.GetFileName(t) == Path.GetFileName(d)))
                    .ToList());
        }

        // finally include the packaged vanilla files, just in case there are no other mods and UXM has never been used to unpack the game
        var includedVanillaFiles = PathHelper.FullyQualifyAppDomainPath("Assets", "VanillaFiles", basePath);
        if (Directory.Exists(includedVanillaFiles))
        {
            files.AddRange(
            Directory.GetFiles(includedVanillaFiles, filter, searchOption)
                .Where(d => !files.Any(t => Path.GetFileName(t) == Path.GetFileName(d)))
                .ToList());
        }

        return files;
    }
}
