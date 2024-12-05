namespace DSLRNet.Core;

using DSLRNet.Core.Config;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Text.RegularExpressions;
using System.Text;
using SixLabors.ImageSharp.Processing;
using Configuration = Configuration;

public partial class IconBuilder(
    IOptionsMonitor<Configuration> configOptions, 
    IOptionsMonitor<IconBuilderSettings> iconSettingsOptions,
    RarityHandler rarityHandler)
{
    private string NameBase = "SB_Icon_DSLR_";

    public async Task ApplyIcons()
    {
        Configuration configuration = configOptions.CurrentValue;
        IconBuilderSettings iconSettings = iconSettingsOptions.CurrentValue;

        string sourcePathBase = iconSettings.ModSourcePath ?? configuration.Settings.GamePath;
        string workPath = Path.Combine(iconSettings.ModSourcePath, "work");

        Directory.CreateDirectory(workPath);

        RarityIconMappingConfig sheetConfig = JsonConvert.DeserializeObject<RarityIconMappingConfig>(File.ReadAllText(Path.Combine("LootIcons", "BakedSheets", "iconmappings.json")));

        // regenerate if needed
        if (iconSettings.RegenerateIconSheets)
        {
            sheetConfig = await RegenerateRaritySheets();
        }
        else
        {
            var preBakedFiles = Directory.GetFiles("LootIcons\\BakedSheets", $"{NameBase}*.dds");
            foreach (var preBakedFile in preBakedFiles)
            {
                var existing = sheetConfig.IconSheets
                    .Single(d => Path.GetFileNameWithoutExtension(d.Name).Equals(Path.GetFileNameWithoutExtension(preBakedFile), StringComparison.OrdinalIgnoreCase));

                existing.GeneratedBytes = File.ReadAllBytes(preBakedFile);
            }
        }

        TPF commonIcons = TPF.Read(Path.Combine(sourcePathBase, "menu", "hi", "01_common.tpf.dcx"));
        var removeIcons = commonIcons.Textures.Where(d => d.Name.Contains(NameBase)).ToList();

        foreach (var item in removeIcons)
        {
            commonIcons.Textures.Remove(item);
        }

        string basePath = Path.GetDirectoryName(commonIcons.Textures.First().Name);

        foreach (var iconSheet in sheetConfig.IconSheets)
        {
            TPF.Texture tex = new TPF.Texture(Path.GetFileNameWithoutExtension(iconSheet.Name).Trim(), 102, 0, iconSheet.GeneratedBytes, TPF.TPFPlatform.PC);
            commonIcons.Textures.Add(tex);
        }

        commonIcons.Write(Path.Combine(configuration.Settings.DeployPath, "menu", "hi", "01_common.tpf.dcx"));

        // save layout file
        SaveLayoutFile(sourcePathBase, configuration.Settings.DeployPath, sheetConfig.IconSheets, iconSettings.IconSheetSettings.IconDimensions);

        rarityHandler.UpdateIconMapping(sheetConfig);

        Directory.Delete(workPath, true);
    }

    private void SaveLayoutFile(string sourcePath, string destinationPath, List<IconSheetParameters> sheets, IconDimensions iconDimensions)
    {
        var fileSource = Path.Combine(sourcePath, "menu", "hi", "01_common.sblytbnd.dcx");

        BND4 bnd = BND4.Read(fileSource);

        var dslrAtlases = bnd.Files.Where(d => d.Name.Contains(NameBase)).ToList();
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

    public Task<RarityIconMappingConfig> RegenerateRaritySheets()
    {
        return Task.FromResult(new RarityIconMappingConfig());

        /*
        // verify withcy path is defined and work path is defined

        // witchy expand 00_solo

        RarityIconMappingConfig sheetConfig = new();

        Regex regex = MyRegex();

        Dictionary<LootType, List<string>> iconsToDuplicate = [];

        iconsToDuplicate[LootType.Weapon] = Directory.GetFiles(hdIconsUiItem.PackageItem.ExpandedDirectory, "MENU_Knowledge_*.dds", SearchOption.AllDirectories)
            .Where(file => sheetConfig.WeaponIcons.Contains(int.Parse(Path.GetFileNameWithoutExtension(file).Replace("MENU_Knowledge_", ""))))
            .ToList();
        iconsToDuplicate[LootType.Armor] = Directory.GetFiles(hdIconsUiItem.PackageItem.ExpandedDirectory, "MENU_Knowledge_*.dds", SearchOption.AllDirectories)
            .Where(file => sheetConfig.ArmorIcons.Contains(int.Parse(Path.GetFileNameWithoutExtension(file).Replace("MENU_Knowledge_", ""))))
            .ToList();
        iconsToDuplicate[LootType.Talisman] = Directory.GetFiles(hdIconsUiItem.PackageItem.ExpandedDirectory, "MENU_Knowledge_*.dds", SearchOption.AllDirectories)
            .Where(file => sheetConfig.TalismanIcons.Contains(int.Parse(Path.GetFileNameWithoutExtension(file).Replace("MENU_Knowledge_", ""))))
            .ToList();

        await this.ConvertToPNG(iconsToDuplicate.Values.SelectMany(s => s).Distinct().Where(d => !File.Exists(Path.ChangeExtension(d, "png"))), hdIconsUiItem.PackageItem.ExpandedDirectory);

        IconMappingConfig iconMappingConfig = new()
        {
            IconSheets = []
        };

        int padding = 2;
        int iconsPerRow = 23;
        int numRows = 12;
        int iconsPerSheet = iconsPerRow * numRows;
        int fullIconWidth = 160;
        int sheetWidth = fullIconWidth * iconsPerRow;
        int sheetHeight = sheetWidth;

        if (File.Exists(Path.Combine(this.WorkDirectory, "iconmappings.json")))
        {
            SaveLayoutFile(JsonConvert.DeserializeObject<IconMappingConfig>(File.ReadAllText(Path.Combine(this.WorkDirectory, "iconmappings.json"))).IconSheets, fullIconWidth, padding);
            return;
        }

        string[] witchyXml = Directory.GetFiles(hdIconsUiItem.PackageItem.ExpandedDirectory, "*witchy*xml");
        string[] lowWitchyXml = Directory.GetFiles(lowIconsUiItem.PackageItem.ExpandedDirectory, "*witchy*xml");

        XDocument xmlDoc = XDocument.Load(witchyXml.First());
        XDocument lowXmlDoc = XDocument.Load(lowWitchyXml.First());

        // Example: Add a new <file> element
        XElement filesElement = xmlDoc.Descendants("files").FirstOrDefault();
        XElement lowFilesElement = lowXmlDoc.Descendants("textures").FirstOrDefault();

        ConcurrentBag<string> ddsConverts = [];

        ConcurrentBag<string> iconSheetFileNames = [];

        List<IconSheetParameters> sheets = new();

        int overallSheetCounter = 1;
        int overallIdCounter = sheetConfig.StartAt;

        foreach (var LootType in iconsToDuplicate.Keys)
        {
            foreach (Rarity rarity in sheetConfig.Rarities)
            {
                IEnumerable<List<string>> splitIcons = iconsToDuplicate[LootType].Split(iconsPerSheet);

                if (rarity.RarityIds.First() == -1 && LootType != LootType.Weapon)
                {
                    continue;
                }

                int iconCounter = 0;

                foreach (List<string> splitItem in splitIcons)
                {
                    IconSheetParameters newItem = new()
                    {
                        Rarity = rarity,
                        Name = $"SB_Icon_DSLR_{LootType}_{overallSheetCounter:D2}.png",
                        IconMappings = new IconMappings()
                        {
                            RarityIds = rarity.RarityIds,
                            IconReplacements = splitItem.Select(s =>
                            {
                                (int x, int y) imageCoordinates = GetImageCoordinates(iconCounter % iconsPerSheet, iconsPerRow, fullIconWidth, fullIconWidth, padding);
                                IconMapping ret = new()
                                {
                                    OriginalIconId = int.Parse(regex.Match(s).Groups[1].Value),
                                    NewIconId = overallIdCounter,
                                    SourceIconPath = Path.GetFileName(s),
                                    TileX = imageCoordinates.x,
                                    TileY = imageCoordinates.y,
                                };

                                overallIdCounter++;
                                iconCounter++;
                                return ret;
                            }).ToList()
                        }
                    };
                    lowFilesElement.Add(new XElement("texture",
                                    new XElement("flags1", "0x00"),
                                    new XElement("format", "102"),
                                    new XElement("name", $"{Path.GetFileNameWithoutExtension(newItem.Name)}.dds")
                                ));
                    overallSheetCounter++;

                    iconMappingConfig.IconSheets.Add(newItem);
                }
            }
        }
        lowXmlDoc.Save(lowWitchyXml.First());

        ConcurrentBag<string> reCompressDirectories = [];

        foreach (var sheetParams in iconMappingConfig.IconSheets)
        {
            var gradient = GetItemGradient(sheetParams.Rarity);

            List<string> iconsNamesForSheet = [];

            foreach (IconMapping icon in sheetParams.IconMappings.IconReplacements)
            {
                string destinationFolder = Path.Combine(hdIconsUiItem.PackageItem.ExpandedDirectory, $"MENU_Knowledge_{icon.NewIconId:D5}.tpf.dcx".ToExpandedDirectoryName());

                Directory.CreateDirectory(destinationFolder);
                string destinationName = Path.Combine(destinationFolder, $"MENU_Knowledge_{icon.NewIconId:D5}.png");
                string sourceDds = Path.Combine(hdIconsUiItem.PackageItem.ExpandedDirectory, $"MENU_Knowledge_{icon.OriginalIconId:D5}.tpf.dcx".ToExpandedDirectoryName(), Path.GetFileName(icon.SourceIconPath));
                string sourcePng = Path.Combine(hdIconsUiItem.PackageItem.ExpandedDirectory, Path.ChangeExtension(Path.GetFileName(icon.SourceIconPath), "png"));
                string generatedPng = Path.Combine(destinationFolder, Path.GetFileName(sourceDds.Replace(".dds", "gradient.png")));
                string destinationPng = generatedPng.Replace("gradient.png", ".png");

                reCompressDirectories.Add(destinationFolder);
                string preBaked = Path.Combine(this.BuildContext.References.UIReplacementsSourceDirectory, "ItemIcons", "PreBaked", $"MENU_Knowledge_{icon.NewIconId:D5}.dds");
                if (File.Exists(preBaked))
                {
                    File.Copy(preBaked, Path.ChangeExtension(destinationName, "dds"), true);
                }
                else
                {
                    // Load the DDS image
                    using (var image = Image.Load<Rgba32>(sourcePng))
                    {
                        // Create a transparent image for compositing
                        using var result = new Image<Rgba32>(image.Width, image.Height);
                        result.Mutate(x => x.BackgroundColor(Color.Transparent));

                        // Composite the gradient onto the result
                        result.Mutate(x => x.DrawImage(gradient, new Point(0, 0), 1f));

                        // Composite the original image onto the result
                        result.Mutate(x => x.DrawImage(image, 1f));

                        // Save the result before resizing
                        result.Save(generatedPng);

                        // Resize and sharpen the result
                        result.Mutate(x => x.Resize(fullIconWidth, fullIconWidth));

                        // Save the final image
                        var lowVersion = destinationName.Replace(".png", ".low.png");
                        result.Save(lowVersion);

                        // Add to the collection
                        iconsNamesForSheet.Add(lowVersion);
                    }

                    File.Copy(generatedPng, destinationName, true);
                }
            }

            string sheetDestination = Path.Combine(lowIconsUiItem.PackageItem.ExpandedDirectory, sheetParams.Name);
            string preBakedSheet = Path.Combine(this.BuildContext.References.UIReplacementsSourceDirectory, "ItemIcons", "PreBaked", sheetParams.Name);
            preBakedSheet = Path.ChangeExtension(preBakedSheet, "dds");

            if (File.Exists(preBakedSheet))
            {
                File.Copy(preBakedSheet, Path.ChangeExtension(sheetDestination, "dds"), true);
            }
            else
            {
                this.CreateMontage(iconsNamesForSheet, sheetDestination, fullIconWidth, iconsPerRow, padding);
                iconSheetFileNames.Add(sheetDestination);
            }

            return ValueTask.CompletedTask;
        }, "Generating icon sheets", maxParallelism: 15);

        await this.ConvertToDDS(ddsConverts, hdIconsUiItem.PackageItem.ExpandedDirectory, "BC7_UNORM");

        foreach (var ddsFile in ddsConverts)
        {
            File.Copy(ddsFile, Path.Combine(this.BuildContext.References.UIReplacementsSourceDirectory, "ItemIcons", "PreBaked", Path.GetFileName(ddsFile)));
        }

        await this.ConvertToDDS(iconSheetFileNames, lowIconsUiItem.PackageItem.ExpandedDirectory, "BC7_UNORM");
        foreach (var iconSheetFileName in iconSheetFileNames)
        {
            File.Copy(iconSheetFileName, Path.Combine(this.BuildContext.References.UIReplacementsSourceDirectory, "ItemIcons", "PreBaked", Path.ChangeExtension(Path.GetFileName(iconSheetFileName), "dds")));
        }

        ddsConverts.ToList().ForEach(d =>
        {
            File.Copy(Path.Combine(hdIconsUiItem.PackageItem.ExpandedDirectory, Path.ChangeExtension(Path.GetFileName(d), "dds")), Path.ChangeExtension(d, "dds"), true);
        });

        await this.ExecuteWitchyBND(reCompressDirectories, "Re-compressing menu icon dcx");

        File.WriteAllText(Path.Combine(this.WorkDirectory, "iconmappings.json"), JsonConvert.SerializeObject(iconMappingConfig));

        // Save the changes back to the XML file
        xmlDoc.Save(witchyXml.First());

        SaveLayoutFile(iconMappingConfig.IconSheets, fullIconWidth, padding);
        */
    }

    public Image<Rgba32> CreateMontage(IconSheetSettings sheetSettings, IEnumerable<Image<Rgba32>> images)
    {
        // Create a blank canvas
        using var canvas = new Image<Rgba32>(sheetSettings.IconSheetSize.Width, sheetSettings.IconSheetSize.Height);

        canvas.Mutate(ctx => ctx.BackgroundColor(Color.Transparent));

        int x = sheetSettings.IconDimensions.Padding;
        int y = sheetSettings.IconDimensions.Padding;
        int count = 0;

        foreach (var image in images)
        {
            // Place the image on the canvas
            canvas.Mutate(d => d.DrawImage(image, new Point(x, y), 1f));

            count += 1;
            var coordinates = GetImageCoordinates(
                count, 
                sheetSettings.IconsPerRow, 
                sheetSettings.IconDimensions.IconSize, 
                sheetSettings.IconDimensions.IconSize, 
                sheetSettings.IconDimensions.Padding);

            x = coordinates.x; y = coordinates.y;
        }

        return canvas;
    }

    //private Image GetItemGradient(Rarity rarity)
    //{
    //    return Image.Load<Rgba32>(Path.Combine(this.BuildContext.References.UIReplacementsSourceDirectory, "ItemIcons", $"{rarity.GradientName}.png"));
    //}

    //public static string ColorToFileNameSafe(System.Drawing.Color color)
    //{
    //    return $"Color_{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
    //}

    static (int x, int y) GetImageCoordinates(int index, int columns, int imageWidth, int imageHeight, int padding)
    {
        int row = index / columns;
        int col = index % columns;

        int x = col * (imageWidth + padding) + padding;
        int y = row * (imageHeight + padding) + padding;

        return (x, y);
    }

    [GeneratedRegex(@"(\d+)\.dds$")]
    private static partial Regex MyRegex();
}
