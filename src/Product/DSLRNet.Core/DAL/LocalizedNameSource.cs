namespace DSLRNet.Core.DAL;

using DSLRNet.Core.Config;
using System.Diagnostics.CodeAnalysis;

public class LocalizedNameSource(
    ILogger<LocalizedNameSource> logger,
    IOptions<Settings> settings, 
    FileSourceHandler fileSourceHandler)
{
    private readonly Dictionary<DataSourceNames, Dictionary<int, string>> nameFileCache = [];

    public bool TryGetNameFromMessageFiles(DataSourceNames param, int id, [NotNullWhen(true)] out string? name)
    {
        name = string.Empty;

        if (nameFileCache.TryGetValue(param, out Dictionary<int, string>? value))
        {
            return value.TryGetValue(id, out name);
        }

        return false;
    }

    public void Initialize()
    {
        nameFileCache[DataSourceNames.EquipParamWeapon] = [];
        nameFileCache[DataSourceNames.EquipParamProtector] = [];
        nameFileCache[DataSourceNames.EquipParamAccessory] = [];

        List<string> gameFMGFiles = fileSourceHandler.ListFilesFromAllModDirectories(Path.Combine("msg", settings.Value.MessageFileLocale), "item*.msgbnd.dcx");

        foreach (string gameMsgFile in gameFMGFiles)
        {
            logger.LogInformation($"Loading message file {Path.GetFileName(gameMsgFile)}");

            BND4 bnd = BND4.Read(gameMsgFile);

            List<BinderFile> weaponNameFiles = bnd.Files.Where(d => d.Name.Contains($"WeaponName")).ToList();
            List<BinderFile> armorNameFiles = bnd.Files.Where(d => d.Name.Contains($"ProtectorName")).ToList();
            List<BinderFile> talismanNameFiles = bnd.Files.Where(d => d.Name.Contains($"AccessoryName")).ToList();

            foreach (var weaponNameFile in weaponNameFiles)
            {
                AddToCache(nameFileCache[DataSourceNames.EquipParamWeapon], FMG.Read(weaponNameFile.Bytes));
            }

            foreach (var armorNameFile in armorNameFiles)
            {
                AddToCache(nameFileCache[DataSourceNames.EquipParamProtector], FMG.Read(armorNameFile.Bytes));
            }

            foreach (var talismanNameFile in talismanNameFiles)
            {
                AddToCache(nameFileCache[DataSourceNames.EquipParamAccessory], FMG.Read(talismanNameFile.Bytes));
            }
        }
    }

    private static void AddToCache(Dictionary<int, string> cache, FMG fmg)
    {
        foreach (FMG.Entry? entry in fmg.Entries)
        {
            var newText = entry.Text ?? string.Empty;

            cache.TryAdd(entry.ID, newText);

            if (cache[entry.ID] == null || cache[entry.ID].Length < newText.Length)
            {
                cache[entry.ID] = newText;
            }
        }
    }
}
