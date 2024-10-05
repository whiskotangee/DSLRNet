namespace DSLRNet.Generators;

using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Handlers;
using Microsoft.Extensions.Options;
using Mods.Common;
using System;
using System.Collections.Generic;

public class TalismanLootGenerator : ParamLootGenerator
{
    public TalismanLootGenerator(
        IOptions<Configuration> configuration,
        RarityHandler rarityHandler,
        WhiteListHandler whitelistHandler,
        SpEffectHandler spEffectHandler,
        RandomNumberGetter random,
        LoreGenerator loreGenerator,
        DamageTypeHandler damageTypeHandler,
        DataRepository dataRepository,
        CumulativeID cumulativeID)
        : base(rarityHandler, whitelistHandler, spEffectHandler, damageTypeHandler, loreGenerator, random, configuration, cumulativeID, dataRepository)
    {
        List<EquipParamAccessory> talismanLoots = CsvLoader.LoadCsv<EquipParamAccessory>("DefaultData\\ER\\CSVs\\EquipParamAccessory.csv");

        this.LoadedLoot = talismanLoots.Select(GenericDictionary.FromObject).ToList();
    }

    public int CreateTalisman(int rarityId = 0, List<int> wllIds = null)
    {
        /*
        // GET THE PARAMS WE NEED
        var talismanParams = GetLootParamDictionaryFromGameType();
        string accessoryGroupParam = talismanParams["talismans_accessorygroupparam"];

        // INITIALISE TALISMAN DESCRIPTION AND SUMMARY
        string talismanDesc = "";
        string talismanSummary = "";

        // CREATE OUR NEW TALISMAN
        var newTalisman = GetLootDictionaryFromId(WhiteListHandler.ChooseLootFromWhitelistId(wllIds, false, true));

        // GET OUR TALISMAN'S CONFIG
        var newTalismanConfig = TalismanConfigs["compiled"][Convert.ToInt32(newTalisman["ID"])];

        // ADD NEW TALISMAN'S DETAILS TO DESCRIPTION AND SUMMARY
        string talismanEffectDesc = GetTalismanConfigEffectDescription(Convert.ToInt32(newTalisman["ID"]));
        string talismanEffectSummary = !string.IsNullOrEmpty(newTalismanConfig["ShortEffect"].ToString()) ? newTalismanConfig["ShortEffect"].ToString() : newTalismanConfig["Effect"].ToString();

        talismanDesc += talismanEffectDesc;
        talismanSummary += talismanEffectSummary + " - ";

        // CHOOSE A SET OF NEW SPEFFECTS BASED ON RARITY
        // STORE HOW MANY FREE SPEFFECT SLOTS OUR PARAM HAS
        var freeSlots = GetAvailableSpeffectSlots(newTalisman);

        // DON'T CONTINUE IF THERE'S NO FREE SLOTS - IF THEY'RE FULL BY DEFAULT THEY'RE PROBABLY NECESSARY FOR THE TALISMAN TO WORK,
        // AND THERE'S NO POINT USING IT AS A BASE IF WE CAN'T REALLY ADD ANYTHING TO IT
        if (freeSlots.Count > 0)
        {
            var fixes = ApplySpeffectsAndStoreFixesArray(rarityId, new List<int> { 0 }, newTalisman, 1.0, true, freeSlots.Count);

            // STORE ORIGINAL TALISMAN NAME
            string originalName = newTalisman["Name"].ToString();
            string finalNameNormal = CreateLootTitle(originalName, rarityId, "", fixes["fixes"]["Suffix"], fixes["fixes"]["Prefix"], fixes["fixes"]["Interfix"], false);

            // SET NEW NAME
            newTalisman["Name"] = finalNameNormal;

            // CREATE FINAL DESCRIPTION AND - IN THIS CASE - SUMMARY
            talismanDesc += fixes["description"];
            talismanSummary += fixes["summary"];

            // ASSIGN NEW ID
            ApplyNextId(newTalisman);

            // SET ACCESSORY GROUP
            SetTalismanGroupVariables(newTalismanConfig, newTalisman, accessoryGroupParam);

            // SET TALISMAN RARITY
            SetLootRarityParamValue(newTalisman, rarityId);

            // EXPORT PARAMETERS
            ExportLootgenParamAndTextToOutputs(newTalisman, finalNameNormal, talismanDesc, talismanSummary, new List<string>(), new List<string> { "-1" });

            return Convert.ToInt32(newTalisman["ID"]);
        }
        */
        return -1;
    }

    /*
    // WE NEED AN EXTRA CUMULATIVE ID FOR "CATEGORY" SO TALISMANS CAN BE STACKED TOGETHER
    public static CumulativeID AccessoryGroupCumulativeID { get; set; }

    // IN ORDER FOR A TALISMAN TO BE USED, WE'LL NEED A CONFIG FOR IT SO WE CAN GET THE ORIGINAL TALISMAN'S EFFECT DESCRIPTION
    public Dictionary<string, object> TalismanConfigs { get; set; } = new Dictionary<string, object>();

    // LOADING FUNCTIONS
    public void LoadTalismanTemplates()
    {
        int talismfEnum = ModManager.MF.P_Talisman;
        LootTemplateLibrary = new Dictionary<string, object>();
        var temporaryLibrary = new Dictionary<string, object>();

        LoadMultipleParamCsvsGettingHeaderFromFirst(ModManager.GetAllFilesInModFolderFromAllMods(talismfEnum, GetAllFilesBannedArray)["result"], temporaryLibrary);

        // LOAD HEADER AND CREATE ARRAYS DICTIONARY IN LTL
        // ONLY CONTINUE IF WE FOUND ANY PARAMS AT ALL - (13/05/23) -> HIJACKING THIS TO LOOK FOR COMPILED DICTIONARIES INSTEAD AFTER REWRITES TO CSV FUNCTION ABOVE
        if (temporaryLibrary.ContainsKey("compiled"))
        {
            LootTemplateLibrary["header"] = temporaryLibrary["header"];
            LootTemplateLibrary["compiled"] = new Dictionary<string, object>();

            // LOAD TALISMANCONFIGS - GET ALL FILES AND SPECIFICALLY FIND FILES WITH THE EXACT NAME OF "TalismanConfig.csv"
            var talisFiles = ModManager.GetAllFilesInModFolderFromAllMods(talismfEnum)["result"];
            var tcFiles = new List<string>();

            foreach (var file in talisFiles)
            {
                var searchKeys = new List<string> { "TalismanConfig.csv", "TalismanConfig.CSV", "talismanconfig.csv", "talismanconfig.CSV" };
                foreach (var key in searchKeys)
                {
                    if (file.Contains(key))
                    {
                        tcFiles.Add(file);
                        break;
                    }
                }
            }

            // IF WE FOUND ANY TALISMAN CONFIGS, LOAD THEM
            if (tcFiles.Count > 0)
            {
                LoadMultipleParamCsvsGettingHeaderFromFirst(tcFiles, TalismanConfigs);

                if (temporaryLibrary.Count > 0 && temporaryLibrary["compiled"].Count > 0)
                {
                    if (TalismanConfigs.Count > 0 && TalismanConfigs["compiled"].Count > 0)
                    {
                        // ITERATE OVER EACH TALISMAN CONFIG WE NOW HAVE, AND IF THERE'S A MATCHING ID IN TEMPORARYLIBRARY'S ARRAYS, ADD THAT ARRAY TO LOOTTEMPLATELIBRARY
                        foreach (var key in temporaryLibrary["compiled"].Keys)
                        {
                            if (HasTalismanConfig(key))
                            {
                                LootTemplateLibrary["compiled"][key] = temporaryLibrary["compiled"][key];
                            }
                        }
                    }
                }
            }

            // WE NEED TALISMAN CONFIGS TO DO ANYTHING (BECAUSE WE NEED THE DESCRIPTIONS) SO CANCEL LOADING IF WE DON'T FIND ANY CONFIG CSVS
            EmitSignal("loottemplatelibrary_loading_complete");
        }
    }
    public int CreateTalisman(int rarityId = 0, List<int> wllIds = null)
    {
        // GET THE PARAMS WE NEED
        var talismanParams = GetLootParamDictionaryFromGameType();
        string accessoryGroupParam = talismanParams["talismans_accessorygroupparam"];

        // INITIALISE TALISMAN DESCRIPTION AND SUMMARY
        string talismanDesc = "";
        string talismanSummary = "";

        // CREATE OUR NEW TALISMAN
        var newTalisman = GetLootDictionaryFromId(WhiteListHandler.ChooseLootFromWhitelistId(wllIds, false, true));

        // GET OUR TALISMAN'S CONFIG
        var newTalismanConfig = TalismanConfigs["compiled"][Convert.ToInt32(newTalisman["ID"])];

        // ADD NEW TALISMAN'S DETAILS TO DESCRIPTION AND SUMMARY
        string talismanEffectDesc = GetTalismanConfigEffectDescription(Convert.ToInt32(newTalisman["ID"]));
        string talismanEffectSummary = !string.IsNullOrEmpty(newTalismanConfig["ShortEffect"].ToString()) ? newTalismanConfig["ShortEffect"].ToString() : newTalismanConfig["Effect"].ToString();

        talismanDesc += talismanEffectDesc;
        talismanSummary += talismanEffectSummary + " - ";

        // CHOOSE A SET OF NEW SPEFFECTS BASED ON RARITY
        // STORE HOW MANY FREE SPEFFECT SLOTS OUR PARAM HAS
        var freeSlots = GetAvailableSpeffectSlots(newTalisman);

        // DON'T CONTINUE IF THERE'S NO FREE SLOTS - IF THEY'RE FULL BY DEFAULT THEY'RE PROBABLY NECESSARY FOR THE TALISMAN TO WORK,
        // AND THERE'S NO POINT USING IT AS A BASE IF WE CAN'T REALLY ADD ANYTHING TO IT
        if (freeSlots.Count > 0)
        {
            var fixes = ApplySpeffectsAndStoreFixesArray(rarityId, new List<int> { 0 }, newTalisman, 1.0, true, freeSlots.Count);

            // STORE ORIGINAL TALISMAN NAME
            string originalName = newTalisman["Name"].ToString();
            string finalNameNormal = CreateLootTitle(originalName, rarityId, "", fixes["fixes"]["Suffix"], fixes["fixes"]["Prefix"], fixes["fixes"]["Interfix"], false);

            // SET NEW NAME
            newTalisman["Name"] = finalNameNormal;

            // CREATE FINAL DESCRIPTION AND - IN THIS CASE - SUMMARY
            talismanDesc += fixes["description"];
            talismanSummary += fixes["summary"];

            // ASSIGN NEW ID
            ApplyNextId(newTalisman);

            // SET ACCESSORY GROUP
            SetTalismanGroupVariables(newTalismanConfig, newTalisman, accessoryGroupParam);

            // SET TALISMAN RARITY
            SetLootRarityParamValue(newTalisman, rarityId);

            // EXPORT PARAMETERS
            ExportLootgenParamAndTextToOutputs(newTalisman, finalNameNormal, talismanDesc, talismanSummary, new List<string>(), new List<string> { "-1" });

            return Convert.ToInt32(newTalisman["ID"]);
        }

        return -1;
    }
    public void SetTalismanGroupVariables(Dictionary<string, object> talisConfig, Dictionary<string, object> talisDict, string accGroupName)
    {
        int accGroup = TalismanCanBeStacked(talisConfig) ? AccessoryGroupCumulativeID.GetNext() : Convert.ToInt32(talisConfig["NoStackingGroupID"]);
        if (talisDict.ContainsKey(accGroupName))
        {
            talisDict[accGroupName] = accGroup;
        }
    }

    // TALISMAN INFORMATION FUNCTIONS

    public bool TalismanCanBeStacked(Dictionary<string, object> talisConfig)
    {
        if (talisConfig.Count == 0)
        {
            return false;
        }
        if (!talisConfig.ContainsKey("NoStackingGroupID"))
        {
            return false;
        }
        return new List<int> { 0, -1 }.Contains(Convert.ToInt32(talisConfig["NoStackingGroupID"]));
    }

    public bool HasTalismanConfig(int id)
    {
        return TalismanConfigs["compiled"].ContainsKey(id);
    }

    public string GetTalismanConfigEffectDescription(int id)
    {
        var gameType = GetGameTypeDictionary()["DSLRDescText"];
        string effectPrefixString = gameType["effect"].ToString();

        if (!HasTalismanConfig(id))
        {
            return string.Empty;
        }

        string effectString = TalismanConfigs["compiled"][id]["Effect"].ToString();
        string stackString = TalismanCanBeStacked(TalismanConfigs["compiled"][id]) ? gameType["nostacking"].ToString() : string.Empty;
        return effectPrefixString + effectString + stackString + Environment.NewLine;
    }

    // MAIN FUNCTIONS

    public void OnModsReady()
    {
        LoadTalismanTemplates();
    }

    public void OnLtlLoaded()
    {
        SetupBannedFromRandomId(ModManager.MF.P_Talisman);
    }

    public void Ready()
    {
        // SETUP MANDATORY PARAM EXPORT KEYS
        var gt = GetLootParamDictionaryFromGameType();
        ParamMandatoryKeys.AddRange(new List<string> { gt["talismans_accessorygroupparam"].ToString() });
        ParamMandatoryKeys.AddRange(GetPassiveSpeffectSlotArrayFromOutputParamName());

        // SETUP LOADING COMPLETE SIGNAL
        LootTemplateLibraryLoadingComplete += OnLtlLoaded;
        ConnectMethodToModManagerSignal("modsready", "OnModsReady");

        AllParamLootSetup();

        // COPY DEFAULT TALISMAN CONFIG
        CreateDefaultBannedFromRandomIdSet(ModManager.MF.P_Talisman, new List<int> { 1032, 1031, 1001, 1002, 1011, 1012, 1021, 1022, 1181, 1171, 1231, 1250, 3001 });
        CopyFile("res://DefaultData/ER/CSVs/TalismanConfig.csv", ModManager.GetModFolderPath(ModManager.MF.P_Talisman) + "/TalismanConfig.csv", false);
        CopyFile("res://DefaultData/ER/CSVs/EquipParamAccessory.csv", ModManager.GetModFolderPath(ModManager.MF.P_Talisman) + "/EquipParamAccessory.csv", false);
    }

    private void LoadMultipleParamCsvsGettingHeaderFromFirst(List<string> result, Dictionary<string, object> library)
    {
        // Placeholder for the actual implementation of load_multiple_param_csvs_getting_header_from_first()
    }

    private bool HasTalismanConfig(string key)
    {
        // Placeholder for the actual implementation of has_talisman_config()
        return false;
    }

    private void EmitSignal(string signal)
    {
        // Placeholder for the actual implementation of emit_signal()
    }
    */
}
