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
using DSLRNet.Core.DAL;

public partial class IconBuilder(
    IOptions<Configuration> configOptions,
    IOptions<Settings> settingsOptions,
    ILogger<IconBuilder> logger,
    RarityHandler rarityHandler,
    DataAccess dataAccess,
    FileSourceHandler fileHandler,
    IOperationProgressTracker progressTracker)
{
    private readonly string nameBase = "SB_Icon_DSLR_";
    private ConcurrentDictionary<string, Image<Bgra32>> loadedDDSImageCache = [];
    private ConcurrentDictionary<string, Image<Rgba32>> loadedPNGImageCache = [];

    public async Task ApplyIcons()
    {
        progressTracker.CurrentStageStepCount = 7;
        progressTracker.CurrentStageProgress = 0;

        logger.LogInformation($"Beginning apply icons");

        Configuration configuration = configOptions.Value;
        Settings settings = settingsOptions.Value;
        IconBuilderSettings iconSettings = settings.IconBuilderSettings;

        string bakedSheetsSource = PathHelper.FullyQualifyAppDomainPath("Assets\\LootIcons\\BakedSheets");
        string iconMappingsFile = Path.Combine(bakedSheetsSource, "iconmappings.json");

        Directory.CreateDirectory(bakedSheetsSource);

        RarityIconMappingConfig sheetConfig = new();

        if (!File.Exists(iconMappingsFile))
        {
            logger.LogInformation("Could not find baked sheets, forcing regeneration of icon files");
            iconSettings.RegenerateIconSheets = true;
        }
        else
        {
            sheetConfig = JsonConvert.DeserializeObject<RarityIconMappingConfig>(File.ReadAllText(iconMappingsFile)) ?? throw new Exception($"Icon mappings file {iconMappingsFile} could not be deserialized to config object");
        }

        // Step 1: Regenerate if needed
        if (iconSettings.RegenerateIconSheets)
        {
            int originalStageCount = progressTracker.CurrentStageStepCount;
            int originalStep = progressTracker.CurrentStageProgress;

            Directory.Delete(bakedSheetsSource, recursive: true);
            Directory.CreateDirectory(bakedSheetsSource);

            logger.LogInformation($"Regenerating icon sheets for each rarity as configured");

            sheetConfig = await RegenerateIconSheets(iconSettings);

            // Save the images to BakedSheets directory
            sheetConfig.IconSheets.ForEach(d =>
            {
                File.WriteAllBytes(Path.Combine(bakedSheetsSource, Path.ChangeExtension(d.Name, "dds")), d.GeneratedBytes);
                d.GeneratedBytes = [];
            });

            // Save the updated icon mappings configuration
            File.WriteAllText(iconMappingsFile, JsonConvert.SerializeObject(sheetConfig, Formatting.Indented));

            progressTracker.CurrentStageStepCount = originalStageCount;
            progressTracker.CurrentStageProgress = originalStep;
            iconSettings.RegenerateIconSheets = false;
        }

        progressTracker.CurrentStageProgress += 1;

        // Step 2: Use the icon sheets from BakedSheets
        logger.LogInformation($"Using baked icon sheet files from {bakedSheetsSource}");

        string[] bakedFiles = Directory.GetFiles(bakedSheetsSource, "*.dds");

        foreach (string bakedFile in bakedFiles)
        {
            logger.LogInformation($"Reading baked icon sheet {Path.GetFileName(bakedFile)}");

            IconSheetParameters? existing = sheetConfig.IconSheets
                .SingleOrDefault(d => Path.GetFileNameWithoutExtension(d.Name).Equals(Path.GetFileNameWithoutExtension(bakedFile), StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.GeneratedBytes = File.ReadAllBytes(bakedFile);
            }
        }

        if (!fileHandler.TryGetFile(Path.Combine("menu", "hi", "01_common.tpf.dcx"), out string sourcePath))
        {
            throw new Exception("Could not find common icon file");
        }

        Directory.CreateDirectory(Path.Combine(settings.DeployPath, "menu", "hi"));

        if (!File.Exists(Path.Combine(settings.DeployPath, "menu", "hi", "01_common.pre-dslr.tpf.dcx")))
        {
            File.Copy(sourcePath, Path.Combine(settings.DeployPath, "menu", "hi", "01_common.pre-dslr.tpf.dcx"));
        }

        logger.LogInformation($"Reading common tpf...");
        TPF commonIcons = TPF.Read(sourcePath);
        List<TPF.Texture> removeIcons = commonIcons.Textures.Where(d => d.Name.Contains(nameBase)).ToList();

        foreach (TPF.Texture? item in removeIcons)
        {
            commonIcons.Textures.Remove(item);
        }

        progressTracker.CurrentStageProgress += 1;

        string basePath = Path.GetDirectoryName(commonIcons.Textures.First().Name) ?? string.Empty;

        foreach (IconSheetParameters iconSheet in sheetConfig.IconSheets)
        {
            logger.LogInformation($"Applying icon sheet {Path.GetFileNameWithoutExtension(iconSheet.Name)}");
            TPF.Texture tex = new(Path.GetFileNameWithoutExtension(iconSheet.Name).Trim(), 102, 0, iconSheet.GeneratedBytes, TPF.TPFPlatform.PC);
            commonIcons.Textures.Add(tex);
            iconSheet.GeneratedBytes = [];
        }

        logger.LogInformation($"Writing common tpf");
        commonIcons.Write(Path.Combine(settings.DeployPath, "menu", "hi", "01_common.tpf.dcx"));

        progressTracker.CurrentStageProgress += 1;
        // save layout file
        SaveLayoutFile(settings.DeployPath, sheetConfig.IconSheets, iconSettings.IconSheetSettings.IconDimensions);
        progressTracker.CurrentStageProgress += 1;

        rarityHandler.UpdateIconMapping(sheetConfig);
        progressTracker.CurrentStageProgress += 1;
    }

    private async Task<RarityIconMappingConfig> RegenerateIconSheets(IconBuilderSettings settings)
    {
        List<TextureAtlas> layoutAtlases = ReadLayoutFiles();

        RarityIconMappingConfig sheetConfig = new()
        { 
            IconSheets = []
        };

        Regex regex = MyRegex();

        if (!fileHandler.TryGetFile(Path.Combine("menu", "hi", "01_common.tpf.dcx"), out string sourcePath))
        {
            throw new Exception("Could not find common icon file");
        }

        TPF baseIcons = TPF.Read(sourcePath);

        Dictionary<LootType, List<ushort>> iconsToDuplicate = [];

        iconsToDuplicate[LootType.Weapon] = dataAccess.EquipParamWeapon.GetAll().Select(s => s.iconId).ToList();
        iconsToDuplicate[LootType.Armor] = dataAccess.EquipParamProtector.GetAll().Select(s => s.iconIdF).ToList().Union(dataAccess.EquipParamProtector.GetAll().Select(s => s.iconIdM).ToList()).ToList();
        iconsToDuplicate[LootType.Talisman] = dataAccess.EquipParamAccessory.GetAll().Select(s => s.iconId).ToList();

        ConcurrentBag<string> iconSheetFileNames = [];

        int overallIdCounter = settings.IconSheetSettings.StartAt;

        ConcurrentBag<IconSheetParameters> generatedSheets = [];

        logger.LogInformation($"Generating icon sheets for {iconsToDuplicate[LootType.Weapon].Count} weapons, {iconsToDuplicate[LootType.Armor].Count} armors, and {iconsToDuplicate[LootType.Talisman].Count} talismans");
        progressTracker.CurrentStageStepCount = iconsToDuplicate.Keys.Count * settings.IconSheetSettings.Rarities.Count;

        await Parallel.ForEachAsync(iconsToDuplicate.Keys, (lootType, c) =>
        {
            int overallSheetCounter = 1;

            foreach (RarityIconDetails rarity in settings.IconSheetSettings.Rarities)
            {
                logger.LogInformation($"Generating icon sheets for loot type {lootType} and icon background{rarity.BackgroundImageName}");

                IEnumerable<List<ushort>> splitIcons = iconsToDuplicate[lootType].Split(settings.IconSheetSettings.GoalIconsPerSheet).ToList();

                if (rarity.RarityIds.First() == -1 && lootType != LootType.Weapon)
                {
                    progressTracker.CurrentStageStepCount += 1;
                    continue;
                }

                Image<Rgba32> iconBackgroundImage = GetItemBackgroundImage(settings, rarity);

                int iconCounter = 0;

                foreach(List<ushort> splitItem in splitIcons)
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
                                    NewIconId = (ushort)overallIdCounter,
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

                progressTracker.CurrentStageStepCount += 1;
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
        Image<Rgba32> result = new(image.Width, image.Height);
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
        (Size size, int iconsPerRow, int totalRows) = CalculateIconSheetSize(sheetSettings, iconMappings.Count);
        // Create a blank canvas
        using Image<Rgba32> canvas = new(size.Width, size.Height);

        canvas.Mutate(ctx => ctx.BackgroundColor(Color.Transparent));

        int count = 0;

        foreach (IconMapping iconMapping in iconMappings)
        {
            if (iconMapping.ConvertedIcon == null)
            {
                throw new Exception($"Failed to generate icon due to {iconMapping.SourceIconPath} not having a converted icon loaded");
            }

            (int x, int y) = GetImageCoordinates(count, sheetSettings, iconsPerRow);

            iconMapping.TileX = x;
            iconMapping.TileY = y;

            canvas.Mutate(d => d.DrawImage(iconMapping.ConvertedIcon, new Point(x, y), 1f));

            count += 1;

            iconMapping.ConvertedIcon?.Dispose();
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

    private List<TextureAtlas> ReadLayoutFiles()
    {
        if (!fileHandler.TryGetFile(Path.Combine("menu", "hi", "01_common.sblytbnd.dcx"), out string sourcePath))
        {
            throw new Exception("Could not find common icon layouts file");
        }

        BND4 bnd = BND4.Read(sourcePath);

        return bnd.Files.Select(d => TextureAtlasSerializer.Deserialize(Encoding.UTF8.GetString(d.Bytes)) ?? throw new Exception($"Could not deserialize layout file {d.Name} to texture atlas object")).ToList();
    }

    private Image<Bgra32> GetOriginalIcon(int iconId, TPF commonIcons, List<TextureAtlas> textureAtlas)
    {
        TextureAtlas? matchingAtlas = null;
        SubTexture? matchingSubTexture = null;

        foreach (TextureAtlas atlas in textureAtlas)
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

        Image<Bgra32> originalIcon = loadedDDSImageCache.GetOrAdd(iconId.ToString(), (n) =>
        {
            Image<Bgra32> matchingIconSheet = loadedDDSImageCache.GetOrAdd(matchingAtlas.ImagePath, (name) =>
            {
                byte[] bytes = commonIcons.Textures.Single(d => Path.GetFileNameWithoutExtension(d.Name) == Path.GetFileNameWithoutExtension(name)).Bytes;
                using MemoryStream memStream = new(bytes);
                using Pfim.IImage ddsImage = Pfim.Pfimage.FromStream(memStream);
                return Image.LoadPixelData<Bgra32>(ddsImage.Data, ddsImage.Width, ddsImage.Height);
            });

            return matchingIconSheet.Clone(ctx => ctx.Crop(new Rectangle(matchingSubTexture.X, matchingSubTexture.Y, matchingSubTexture.Width, matchingSubTexture.Height)));
        });

        return originalIcon;
    }

    private void SaveLayoutFile(string destinationPath, List<IconSheetParameters> sheets, IconDimensions iconDimensions)
    {
        logger.LogInformation($"Saving layout file for new icon sheets at {destinationPath}");

        if (!fileHandler.TryGetFile(Path.Combine("menu", "hi", "01_common.sblytbnd.dcx"), out string fileSource))
        {
            throw new Exception("Could not find common icon layout file");
        }

        string fileDestination = Path.Combine(destinationPath, "menu", "hi", "01_common.sblytbnd.dcx");
        string preDSLRFile = fileDestination.Replace(".dcx", "pre-dslr.dcx");

        if (!File.Exists(preDSLRFile))
        {
            File.Copy(fileSource, preDSLRFile);
        }

        fileSource = preDSLRFile;

        BND4 bnd = BND4.Read(fileSource);

        List<BinderFile> dslrAtlases = bnd.Files.Where(d => d.Name.Contains(nameBase)).ToList();
        foreach (BinderFile? atlas in dslrAtlases)
        {
            bnd.Files.Remove(atlas);
        }

        string? pathBase = Path.GetDirectoryName(bnd.Files.Last().Name) ?? string.Empty;
        int id = bnd.Files.Last().ID + 1;

        foreach (IconSheetParameters sheet in sheets)
        {
            string sheetName = Path.GetFileNameWithoutExtension(sheet.Name);

            List<IconMapping> items = sheet.IconMappings.IconReplacements;
            TextureAtlas newAtlas = new()
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

        bnd.Write(fileDestination);
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
            Image<Rgba32> image = Image.Load<Rgba32>(rarity.BackgroundImageName);
            image.Mutate(x => x.Resize(new Size(settings.IconSheetSettings.IconDimensions.IconSize)));
            return image;
        });
    }
    
    private static (int x, int y) GetImageCoordinates(int index, IconSheetSettings settings, int iconsPerRow)
    {
        int columns = iconsPerRow;

        int row = index / columns;
        int col = index % columns;

        int x = col * (settings.IconDimensions.IconSize + settings.IconDimensions.Padding) + settings.IconDimensions.Padding;
        int y = row * (settings.IconDimensions.IconSize + settings.IconDimensions.Padding) + settings.IconDimensions.Padding;

        return (x, y);
    }

    private void ClearCaches()
    {
        foreach (Image<Bgra32> image in loadedDDSImageCache.Values)
        {
            image.Dispose();
        }

        foreach (Image<Rgba32> image in loadedPNGImageCache.Values)
        {
            image.Dispose();
        }

        loadedDDSImageCache = [];
        loadedPNGImageCache = [];
    }

    [GeneratedRegex(@"(\d+)\.dds$")]
    private static partial Regex MyRegex();
}
