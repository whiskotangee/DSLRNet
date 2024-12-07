namespace DSLRNet.Core;

using DSLRNet.Core.Config;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Text.RegularExpressions;
using System.Text;
using SixLabors.ImageSharp.Processing;
using Configuration = Configuration;
using System.Collections.Concurrent;
using System.Linq;
using DSLRNet.Core.Extensions;
using DdsFileTypePlus;
using PaintDotNet;
using ImageMagick;

public partial class IconBuilder(
    IOptionsMonitor<Configuration> configOptions, 
    IOptionsMonitor<IconBuilderSettings> iconSettingsOptions,
    ILogger<IconBuilder> logger,
    RarityHandler rarityHandler,
    ProcessRunner processRunner)
{
    private readonly string nameBase = "SB_Icon_DSLR_";
    private ConcurrentDictionary<string, Image<Bgra32>> loadedImageCache = [];

    public async Task ApplyIcons()
    {
        logger.LogInformation($"Beginning apply icons");

        Configuration configuration = configOptions.CurrentValue;
        IconBuilderSettings iconSettings = iconSettingsOptions.CurrentValue;

        string sourcePathBase = iconSettings.ModSourcePath ?? configuration.Settings.GamePath;
        string workPath = Path.Combine(configuration.Settings.DeployPath, "work");
        string bakedSheetsSource = $"{iconSettings.IconSourcePath}\\BakedSheets";
        string preBakedSheetsSource = $"{iconSettings.IconSourcePath}\\PreBakedSheets";
        string iconMappingsFile = Path.Combine(bakedSheetsSource, "iconmappings.json");

        Directory.CreateDirectory(workPath);
        Directory.CreateDirectory(bakedSheetsSource);

        // Check if there are no DDS files in bakedSheetsSource
        if (Directory.GetFiles(bakedSheetsSource, "*.dds").Length == 0)
        {
            // Copy everything from PreBakedSheets to BakedSheets
            var preBakedFiles = Directory.GetFiles(preBakedSheetsSource, "*.dds");
            foreach (var preBakedFile in preBakedFiles)
            {
                string destinationFile = Path.Combine(bakedSheetsSource, Path.GetFileName(preBakedFile));
                File.Copy(preBakedFile, destinationFile, overwrite: true);
            }
        }

        if (!File.Exists(iconMappingsFile))
        {
            File.Copy(Path.Combine(preBakedSheetsSource, Path.GetFileName(iconMappingsFile)), iconMappingsFile);
        }

        RarityIconMappingConfig sheetConfig = JsonConvert.DeserializeObject<RarityIconMappingConfig>(File.ReadAllText(iconMappingsFile));

        // Step 1: Regenerate if needed
        if (iconSettings.RegenerateIconSheets)
        {
            logger.LogInformation($"Regenerating icon sheets for each rarity as configured");

            sheetConfig = await RegenerateRaritySheets(configuration, iconSettings, sourcePathBase, workPath);

            // Save the images to BakedSheets directory
            sheetConfig.IconSheets.ForEach(d =>
            {
                File.WriteAllBytes(Path.Combine(bakedSheetsSource, Path.ChangeExtension(d.Name, "dds")), d.GeneratedBytes);
                d.GeneratedBytes = [];
            });

            // Save the updated icon mappings configuration
            File.WriteAllText(iconMappingsFile, JsonConvert.SerializeObject(sheetConfig, Formatting.Indented));

            Directory.Delete(workPath, true);
        }

        // Step 2: Use the icon sheets from BakedSheets
        logger.LogInformation($"Using baked icon sheet files from {bakedSheetsSource}");

        var bakedFiles = Directory.GetFiles(bakedSheetsSource, "*.dds");

        foreach (var bakedFile in bakedFiles)
        {
            var existing = sheetConfig.IconSheets
                .SingleOrDefault(d => Path.GetFileNameWithoutExtension(d.Name).Equals(Path.GetFileNameWithoutExtension(bakedFile), StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.GeneratedBytes = File.ReadAllBytes(bakedFile);
            }
        }

        TPF commonIcons = TPF.Read(Path.Combine(sourcePathBase, "menu", "hi", "01_common.tpf.dcx"));
        var removeIcons = commonIcons.Textures.Where(d => d.Name.Contains(nameBase)).ToList();

        foreach (var item in removeIcons)
        {
            commonIcons.Textures.Remove(item);
        }

        string basePath = Path.GetDirectoryName(commonIcons.Textures.First().Name);

        foreach (var iconSheet in sheetConfig.IconSheets)
        {
            TPF.Texture tex = new(Path.GetFileNameWithoutExtension(iconSheet.Name).Trim(), 102, 0, iconSheet.GeneratedBytes, TPF.TPFPlatform.PC);
            commonIcons.Textures.Add(tex);
            iconSheet.GeneratedBytes = [];
        }

        commonIcons.Write(Path.Combine(configuration.Settings.DeployPath, "menu", "hi", "01_common.tpf.dcx"));

        // save layout file
        SaveLayoutFile(sourcePathBase, configuration.Settings.DeployPath, sheetConfig.IconSheets, iconSettings.IconSheetSettings.IconDimensions);

        rarityHandler.UpdateIconMapping(sheetConfig);
    }

    private void SaveLayoutFile(string sourcePath, string destinationPath, List<IconSheetParameters> sheets, IconDimensions iconDimensions)
    {
        logger.LogInformation($"Saving layout file for new icon sheets at {destinationPath}");

        var fileSource = Path.Combine(sourcePath, "menu", "hi", "01_common.sblytbnd.dcx");

        BND4 bnd = BND4.Read(fileSource);

        var dslrAtlases = bnd.Files.Where(d => d.Name.Contains(nameBase)).ToList();
        foreach(var atlas in dslrAtlases)
        {
            bnd.Files.Remove(atlas);
        }

        string? pathBase = Path.GetDirectoryName(bnd.Files.Last().Name);
        int id = bnd.Files.Last().ID + 1;

        foreach (IconSheetParameters sheet in sheets)
        {
            string sheetName = Path.GetFileNameWithoutExtension(sheet.Name);

            List<IconMapping> items = sheet.IconMappings.IconReplacements;
            string sheetXml = $"<TextureAtlas imagePath=\"{Path.GetFileNameWithoutExtension(sheet.Name)}.png\">";
            sheetXml += string.Join(" ", items.Select(s => $"\t<SubTexture name=\"MENU_ItemIcon_{s.NewIconId:D5}.png\" x=\"{s.TileX}\" y=\"{s.TileY}\" width=\"{iconDimensions.IconSize}\" height=\"{iconDimensions.IconSize}\" half=\"0\"/>").ToList());
            sheetXml += "</TextureAtlas>";

            bnd.Files.Add(
                new BinderFile(Binder.FileFlags.Flag1, id, Path.Combine(pathBase, $"{Path.GetFileNameWithoutExtension(sheet.Name)}.layout"), Encoding.UTF8.GetBytes(sheetXml)));
            id++;
        }

        bnd.Write(Path.Combine(destinationPath, "menu", "hi", "01_common.sblytbnd.dcx"));
    }

    public async Task<RarityIconMappingConfig> RegenerateRaritySheets(Configuration config, IconBuilderSettings settings, string sourcePath, string workPath)
    {
        var sourceFile = Path.Combine(sourcePath, "menu", "hi", "00_solo.tpfbdt");
        var headerFile = Path.Combine(sourcePath, "menu", "hi", "00_solo.tpfbhd");

        File.Copy(sourceFile, Path.Combine(workPath, Path.GetFileName(sourceFile)), true);
        File.Copy(headerFile, Path.Combine(workPath, Path.GetFileName(headerFile)), true);

        var workFile = Path.Combine(workPath, Path.GetFileName(sourceFile));
        var expandedDirectory = workFile.Replace(".", "-");

        await processRunner.RunProcessAsync(new ProcessRunnerArgs<string>()
        {
            ExePath = config.Settings.WitchyBNDPath,
            Arguments = $"{workFile} --silent --recursive"
        });

        RarityIconMappingConfig sheetConfig = new()
        { 
            IconSheets = []
        };

        Regex regex = MyRegex();

        Dictionary<LootType, List<string>> iconsToDuplicate = [];

        iconsToDuplicate[LootType.Weapon] = Directory.GetFiles(expandedDirectory, "MENU_Knowledge_*.dds", SearchOption.AllDirectories)
            .Where(file => settings.IconSheetSettings.WeaponIcons.Contains(int.Parse(Path.GetFileNameWithoutExtension(file).Replace("MENU_Knowledge_", ""))))
            .ToList();
        iconsToDuplicate[LootType.Armor] = Directory.GetFiles(expandedDirectory, "MENU_Knowledge_*.dds", SearchOption.AllDirectories)
            .Where(file => settings.IconSheetSettings.ArmorIcons.Contains(int.Parse(Path.GetFileNameWithoutExtension(file).Replace("MENU_Knowledge_", ""))))
            .ToList();
        iconsToDuplicate[LootType.Talisman] = Directory.GetFiles(expandedDirectory, "MENU_Knowledge_*.dds", SearchOption.AllDirectories)
            .Where(file => settings.IconSheetSettings.TalismanIcons.Contains(int.Parse(Path.GetFileNameWithoutExtension(file).Replace("MENU_Knowledge_", ""))))
            .ToList();
        ConcurrentBag<string> iconSheetFileNames = [];

        int overallIdCounter = settings.IconSheetSettings.StartAt;
        var iconsPerSheet = settings.IconSheetSettings.RowsPerSheet * settings.IconSheetSettings.IconsPerRow;

        ConcurrentBag<IconSheetParameters> generatedSheets = [];

        logger.LogInformation($"Generating icon sheets for {iconsToDuplicate[LootType.Weapon].Count} weapons, {iconsToDuplicate[LootType.Armor].Count} armors, and {iconsToDuplicate[LootType.Talisman].Count} talismans");
        await Parallel.ForEachAsync(iconsToDuplicate.Keys, (lootType, c) =>
        {
            int overallSheetCounter = 1;

            foreach (var rarity in settings.IconSheetSettings.Rarities)
            {
                logger.LogInformation($"Generating icon sheets for loot type {lootType} and icon background{rarity.BackgroundImageName}");

                IEnumerable<List<string>> splitIcons = iconsToDuplicate[lootType].Split(iconsPerSheet).ToList();

                if (rarity.RarityIds.First() == -1 && lootType != LootType.Weapon)
                {
                    continue;
                }

                using Image<Rgba32> iconBackgroundImage = GetItemBackgroundImage(settings, rarity);

                int iconCounter = 0;

                foreach (List<string> splitItem in splitIcons)
                {
                    IconSheetParameters newItem = new()
                    {
                        Name = $"{nameBase}{lootType}_{overallSheetCounter:D2}.png",
                        IconMappings = new RarityIconMapping()
                        {
                            RarityIds = rarity.RarityIds,
                            IconReplacements = splitItem.Select(s =>
                            {
                                (int x, int y) = GetImageCoordinates(iconCounter % iconsPerSheet, settings.IconSheetSettings);
                                IconMapping ret = new()
                                {
                                    OriginalIconId = int.Parse(regex.Match(s).Groups[1].Value),
                                    NewIconId = overallIdCounter,
                                    SourceIconPath = Path.GetFileName(s),
                                    TileX = x,
                                    TileY = y,
                                    ConvertedIcon = CreateIcon(settings.IconSheetSettings, iconBackgroundImage, s)
                                };

                                Interlocked.Increment(ref overallIdCounter);
                                iconCounter++;
                                return ret;
                            }).ToList()
                        }
                    };

                    // create montage, keep loaded bytes in return value
                    overallSheetCounter++;

                    logger.LogInformation($"Creating montage image for {newItem.Name} with {newItem.IconMappings.IconReplacements.Count} icons");

                    newItem.GeneratedBytes = CreateMontage(settings.IconSheetSettings, newItem.IconMappings.IconReplacements);

                    logger.LogInformation($"Completed creating montage image for {newItem.Name}");

                    generatedSheets.Add(newItem);
                }
            }

            return ValueTask.CompletedTask;
        });

        sheetConfig.IconSheets.AddRange(generatedSheets);

        foreach (var image in loadedImageCache.Values)
        {
            image.Dispose();
        }

        loadedImageCache = [];

        return sheetConfig;
    }

    public Image<Rgba32> CreateIcon(IconSheetSettings settings, Image<Rgba32> gradient, string baseIconFile)
    {
        Image<Bgra32> image = this.loadedImageCache.GetOrAdd(baseIconFile, (name) =>
        {
            using var ddsImage = Pfim.Pfimage.FromFile(name);
            return Image.LoadPixelData<Bgra32>(ddsImage.Data, ddsImage.Width, ddsImage.Height);
        });

        // Create a transparent image for compositing
        var result = new Image<Rgba32>(image.Width, image.Height);
        result.Mutate(x => x.BackgroundColor(Color.Transparent));

        // Composite the background image onto the result
        result.Mutate(x => x.DrawImage(gradient, new Point(0, 0), 1f));

        // Composite the original image onto the result
        result.Mutate(x => x.DrawImage(image, 1f));

        // Resize and sharpen the result
        result.Mutate(x => x.Resize(settings.IconDimensions.IconSize, settings.IconDimensions.IconSize));

        return result;
    }

    public byte[] CreateMontage(IconSheetSettings sheetSettings, List<IconMapping> iconMappings)
    {
        // Create a blank canvas
        using var canvas = new Image<Rgba32>(sheetSettings.IconSheetSize.Width, sheetSettings.IconSheetSize.Height);

        canvas.Mutate(ctx => ctx.BackgroundColor(Color.Transparent));

        int x = sheetSettings.IconDimensions.Padding;
        int y = sheetSettings.IconDimensions.Padding;
        int count = 0;

        foreach (var iconMapping in iconMappings)
        {
            x = iconMapping.TileX; y = iconMapping.TileY;

            canvas.Mutate(d => d.DrawImage(iconMapping.ConvertedIcon, new Point(x, y), 1f));

            count += 1;

            iconMapping.ConvertedIcon.Dispose();
            iconMapping.ConvertedIcon = null;
        }

        using MemoryStream pngStream = new();
        canvas.SaveAsPng(pngStream);
        pngStream.Position = 0;
        
        MagickImage pngConverter = new(pngStream, MagickFormat.Png);
        using MemoryStream magickDDSStream = new();
        pngConverter.Write(magickDDSStream, MagickFormat.Dds);

        using Surface ddsSurface = DdsFile.Load(magickDDSStream.ToArray());
        using MemoryStream ddsStream = new();

        DdsFile.Save(ddsStream, DdsFileFormat.BC7, DdsErrorMetric.Perceptual, BC7CompressionSpeed.Fast,
            false, false, ResamplingAlgorithm.Bicubic, ddsSurface, null);

        return ddsStream.ToArray();
    }

    private Image<Rgba32> GetItemBackgroundImage(IconBuilderSettings settings, RarityIconDetails rarity)
    {
        return Image.Load<Rgba32>(Path.Combine(settings.IconSourcePath, $"{rarity.BackgroundImageName}.png"));
    }
    
    private static (int x, int y) GetImageCoordinates(int index, IconSheetSettings settings)
    {
        var columns = settings.IconsPerRow;

        int row = index / columns;
        int col = index % columns;

        int x = col * (settings.IconDimensions.IconSize + settings.IconDimensions.Padding) + settings.IconDimensions.Padding;
        int y = row * (settings.IconDimensions.IconSize + settings.IconDimensions.Padding) + settings.IconDimensions.Padding;

        return (x, y);
    }

    [GeneratedRegex(@"(\d+)\.dds$")]
    private static partial Regex MyRegex();
}
