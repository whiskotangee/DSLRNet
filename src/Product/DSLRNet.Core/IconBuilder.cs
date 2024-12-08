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
    ProcessRunner processRunner,
    IDataSource<EquipParamWeapon> weaponDataSource,
    IDataSource<EquipParamProtector> armorDataSource,
    IDataSource<EquipParamAccessory> accessoryDataSource)
{
    private readonly string nameBase = "SB_Icon_DSLR_";
    private ConcurrentDictionary<string, Image<Bgra32>> loadedDDSImageCache = [];
    private ConcurrentDictionary<string, Image<Rgba32>> loadedPNGImageCache = [];

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
            Directory.Delete(bakedSheetsSource, recursive: true);
            Directory.CreateDirectory(bakedSheetsSource);

            logger.LogInformation($"Regenerating icon sheets for each rarity as configured");

            sheetConfig = await RegenerateIconSheets(iconSettings, sourcePathBase, workPath);

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

    private async Task<RarityIconMappingConfig> RegenerateIconSheets(IconBuilderSettings settings, string sourcePath, string workPath)
    {
        var layoutAtlases = ReadLayoutFiles(sourcePath);

        RarityIconMappingConfig sheetConfig = new()
        { 
            IconSheets = []
        };

        Regex regex = MyRegex();

        TPF baseIcons = TPF.Read(Path.Combine(sourcePath, "menu", "hi", "01_common.tpf.dcx"));

        Dictionary<LootType, List<int>> iconsToDuplicate = [];

        iconsToDuplicate[LootType.Weapon] = weaponDataSource.GetAll().Select(s => s.iconId).ToList();
        iconsToDuplicate[LootType.Armor] = armorDataSource.GetAll().Select(s => s.iconIdF).ToList().Union(armorDataSource.GetAll().Select(s => s.iconIdM).ToList()).ToList();
        iconsToDuplicate[LootType.Talisman] = accessoryDataSource.GetAll().Select(s => s.iconId).ToList();

        ConcurrentBag<string> iconSheetFileNames = [];

        int overallIdCounter = settings.IconSheetSettings.StartAt;

        ConcurrentBag<IconSheetParameters> generatedSheets = [];

        logger.LogInformation($"Generating icon sheets for {iconsToDuplicate[LootType.Weapon].Count} weapons, {iconsToDuplicate[LootType.Armor].Count} armors, and {iconsToDuplicate[LootType.Talisman].Count} talismans");
        await Parallel.ForEachAsync(iconsToDuplicate.Keys, (lootType, c) =>
        {
            int overallSheetCounter = 1;

            foreach (var rarity in settings.IconSheetSettings.Rarities)
            {
                logger.LogInformation($"Generating icon sheets for loot type {lootType} and icon background{rarity.BackgroundImageName}");

                IEnumerable<List<int>> splitIcons = iconsToDuplicate[lootType].Split(settings.IconSheetSettings.GoalIconsPerSheet).ToList();

                if (rarity.RarityIds.First() == -1 && lootType != LootType.Weapon)
                {
                    continue;
                }

                Image<Rgba32> iconBackgroundImage = GetItemBackgroundImage(settings, rarity);

                int iconCounter = 0;

                foreach(var splitItem in splitIcons)
                {
                    IconSheetParameters newItem = new()
                    {
                        Name = $"{nameBase}{lootType}_{overallSheetCounter:D2}.png",
                        IconMappings = new RarityIconMapping()
                        {
                            RarityIds = rarity.RarityIds,
                            IconReplacements = splitItem.Select(s =>
                            {
                                IconMapping ret = new()
                                {
                                    OriginalIconId = s,
                                    NewIconId = overallIdCounter,
                                    SourceIconPath = $"MENU_ItemIcon_{s:D5}.png",
                                    ConvertedIcon = CreateIcon(settings.IconSheetSettings, iconBackgroundImage, s, baseIcons, layoutAtlases)
                                };

                                Interlocked.Increment(ref overallIdCounter);
                                Interlocked.Increment(ref iconCounter);
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

        ClearCaches();

        return sheetConfig;
    }

    private Image<Rgba32> CreateIcon(IconSheetSettings settings, Image<Rgba32> gradient, int baseIconId, TPF commonIcons, List<TextureAtlas> textureAtlases)
    {
        Image<Bgra32> image = this.GetOriginalIcon(baseIconId, commonIcons, textureAtlases);

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

    private byte[] CreateMontage(IconSheetSettings sheetSettings, List<IconMapping> iconMappings)
    {
        var iconSheetSizeDetails = CalculateIconSheetSize(sheetSettings, iconMappings.Count);
        // Create a blank canvas
        using var canvas = new Image<Rgba32>(iconSheetSizeDetails.size.Width, iconSheetSizeDetails.size.Height);

        canvas.Mutate(ctx => ctx.BackgroundColor(Color.Transparent));

        int count = 0;

        foreach (var iconMapping in iconMappings)
        {
            (int x, int y) = GetImageCoordinates(count, sheetSettings, iconSheetSizeDetails.iconsPerRow);

            iconMapping.TileX = x;
            iconMapping.TileY = y;

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

    private List<TextureAtlas> ReadLayoutFiles(string sourcePath)
    {
        var fileSource = Path.Combine(sourcePath, "menu", "hi", "01_common.sblytbnd.dcx");

        BND4 bnd = BND4.Read(fileSource);

        return bnd.Files.Select(d => TextureAtlasSerializer.Deserialize(Encoding.UTF8.GetString(d.Bytes))).ToList();
    }

    private Image<Bgra32> GetOriginalIcon(int iconId, TPF commonIcons, List<TextureAtlas> textureAtlas)
    {
        TextureAtlas? matchingAtlas = null;
        SubTexture? matchingSubTexture = null;

        foreach (var atlas in textureAtlas)
        {
            matchingSubTexture = atlas.SubTextures.SingleOrDefault(d => Path.GetFileNameWithoutExtension(d.Name).EndsWith(iconId.ToString("D5")));

            if (matchingSubTexture != null)
            {
                matchingAtlas = atlas;
                break;
            }
        }

        if (matchingAtlas == null || matchingSubTexture == null)
        {
            throw new Exception("Could not find icon based on icon Id in known texture atlases");
        }

        var originalIcon = loadedDDSImageCache.GetOrAdd(iconId.ToString(), (n) =>
        {
            var matchingIconSheet = loadedDDSImageCache.GetOrAdd(matchingAtlas.ImagePath, (name) =>
            {
                var bytes = commonIcons.Textures.Single(d => Path.GetFileNameWithoutExtension(d.Name) == Path.GetFileNameWithoutExtension(name)).Bytes;
                using var memStream = new MemoryStream(bytes);
                using var ddsImage = Pfim.Pfimage.FromStream(memStream);
                return Image.LoadPixelData<Bgra32>(ddsImage.Data, ddsImage.Width, ddsImage.Height);
            });

            return matchingIconSheet.Clone(ctx => ctx.Crop(new Rectangle(matchingSubTexture.X, matchingSubTexture.Y, matchingSubTexture.Width, matchingSubTexture.Height)));
        });

        return originalIcon;
    }

    private void SaveLayoutFile(string sourcePath, string destinationPath, List<IconSheetParameters> sheets, IconDimensions iconDimensions)
    {
        logger.LogInformation($"Saving layout file for new icon sheets at {destinationPath}");

        var fileSource = Path.Combine(sourcePath, "menu", "hi", "01_common.sblytbnd.dcx");

        BND4 bnd = BND4.Read(fileSource);

        var dslrAtlases = bnd.Files.Where(d => d.Name.Contains(nameBase)).ToList();
        foreach (var atlas in dslrAtlases)
        {
            bnd.Files.Remove(atlas);
        }

        string? pathBase = Path.GetDirectoryName(bnd.Files.Last().Name);
        int id = bnd.Files.Last().ID + 1;

        foreach (IconSheetParameters sheet in sheets)
        {
            string sheetName = Path.GetFileNameWithoutExtension(sheet.Name);

            List<IconMapping> items = sheet.IconMappings.IconReplacements;
            var newAtlas = new TextureAtlas()
            {
                ImagePath = $"{sheetName}.png",
                SubTextures = items
                    .Select(i => new SubTexture()
                    {
                        Name = $"MENU_ItemIcon_{i.NewIconId:D5}.png",
                        X = i.TileX,
                        Y = i.TileY,
                        Width = iconDimensions.IconSize,
                        Height = iconDimensions.IconSize,
                        Half = 0
                    }).ToList()
            };

            bnd.Files.Add(
                new BinderFile(Binder.FileFlags.Flag1, id, Path.Combine(pathBase, $"{Path.GetFileNameWithoutExtension(sheet.Name)}.layout"), Encoding.UTF8.GetBytes(TextureAtlasSerializer.Serialize(newAtlas))));
            id++;
        }

        bnd.Write(Path.Combine(destinationPath, "menu", "hi", "01_common.sblytbnd.dcx"));
    }

    private (Size size, int iconsPerRow, int totalRows) CalculateIconSheetSize(IconSheetSettings settings, int iconCount)
    {
        int iconSize = settings.IconDimensions.IconSize;
        int padding = settings.IconDimensions.Padding;

        int minArea = int.MaxValue;
        int bestIconsPerRow = 0;
        int bestTotalRows = 0;
        int bestWidth = 0;
        int bestHeight = 0;

        for (int iconsPerRow = iconCount; iconsPerRow > 0; iconsPerRow--)
        {
            int totalRows = (int)Math.Ceiling((double)iconCount / iconsPerRow);

            int width = iconsPerRow * (iconSize + padding) - padding;
            int height = totalRows * (iconSize + padding) - padding;

            width = MathFunctions.NextPowerOfTwo(width);
            height = MathFunctions.NextPowerOfTwo(height);

            int area = width * height;
            int aspectRatioDifference = Math.Abs(width - height);

            if (area < minArea || (area == minArea && aspectRatioDifference < Math.Abs(bestWidth - bestHeight)))
            {
                minArea = area;
                bestIconsPerRow = iconsPerRow;
                bestTotalRows = totalRows;
                bestWidth = width;
                bestHeight = height;
            }
        }

        return (new Size(bestWidth, bestHeight), bestIconsPerRow, bestTotalRows);
    }

    private Image<Rgba32> GetItemBackgroundImage(IconBuilderSettings settings, RarityIconDetails rarity)
    {
        return loadedPNGImageCache.GetOrAdd(rarity.BackgroundImageName, (name) =>
        {
            var image = Image.Load<Rgba32>(Path.Combine(settings.IconSourcePath, $"{rarity.BackgroundImageName}.png"));
            image.Mutate(x => x.Resize(new Size(settings.IconSheetSettings.IconDimensions.IconSize)));
            return image;
        });
    }
    
    private static (int x, int y) GetImageCoordinates(int index, IconSheetSettings settings, int iconsPerRow)
    {
        var columns = iconsPerRow;

        int row = index / columns;
        int col = index % columns;

        int x = col * (settings.IconDimensions.IconSize + settings.IconDimensions.Padding) + settings.IconDimensions.Padding;
        int y = row * (settings.IconDimensions.IconSize + settings.IconDimensions.Padding) + settings.IconDimensions.Padding;

        return (x, y);
    }

    private void ClearCaches()
    {
        foreach (var image in loadedDDSImageCache.Values)
        {
            image.Dispose();
        }

        foreach (var image in loadedPNGImageCache.Values)
        {
            image.Dispose();
        }

        loadedDDSImageCache = [];
        loadedPNGImageCache = [];
    }

    [GeneratedRegex(@"(\d+)\.dds$")]
    private static partial Regex MyRegex();
}
