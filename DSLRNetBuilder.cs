using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Generators;
using DSLRNet.Handlers;
using Microsoft.Extensions.Options;
using Mods.Common;

namespace DSLRNet;

public class DSLRNetBuilder(
    ItemLotGenerator itemLotGenerator,
    IOptions<Configuration> configuration,
    DataRepository dataRepository)
{
    private readonly Configuration configuration = configuration.Value;

    public void BuildAndApply()
    {
        // get all queue entries

        // TODO: Loading ini is failing due to [] in values
        var enemyItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Enemies", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, this.configuration.Itemlots.ParamCategories[0]));

        var mapItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Map", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, this.configuration.Itemlots.ParamCategories[1]));

        // ItemLotGenerator
        // do enemies
        // do map finds?
        // generate itemlots for each type
        // armor
        // weapon
        // talismans
        // Get/Generator massedit
        // dsms apply

        itemLotGenerator.CreateItemLots(enemyItemLotsSetups.Union(mapItemLotsSetups));

        var generatedData = dataRepository.GetMassEditContents();

        File.WriteAllLines("apply.massedit", generatedData);

        // TODO: write msgbnd

        // launch dsmsportable
    }
}

/*
    //ALL MOD FOLDERS
    const ModFolders : Array[String] = new Array() { "WhitelistedLoot","Itemlots_Map","Itemlots_Enemies","Param_Weapons","Param_Armor","Param_Talisman","Param_SpEffects","Lore_Generation","Rarities","DamageTypes","ExtraParts"};
    enum MF { WhitelistedLoot, ItemLotParam_map, ItemLotParam_enemy, P_Weapons, P_Armor, P_Talisman, P_SpEffects, Lore, Rarities, DamageType, ExtraParts }

    //WEAPON FOLDERS - REPLACED WITH A CONST IN PARAMLOOT WEAPONS NODE SCRIPT AS OF 09/04/23
    enum WF { Normal, Shields, StaffsSeals, BowsCrossbows }
    //MAKE SURE TO UPDATE THIS AS MODFOLDERS CHANGE!!

    //LOADED MOD INFO ARRAYS

    Array ItemlotIDBlacklist[int];

    //LOAD ORDER

    public Array LoadOrder = new Array() { };

    //EXTRA PARTS FOLDERS

    public Array ExtraParts = new Array() { };

    //LOADED MOD TEMPLATE

    public Dictionary LoadedMods = new Dictionary() { };

    //MODS LOADED SIGNAL

    public signal modsready;
    public signal dirsready;

    public void _ready()
    {
        startup_modmanager();

    }

    public void startup_modmanager()
    {
        create_default_mod_directory();
        if (game_is_elden_ring())
        {
            create_default_configs();
        }
        signal_x_ready("dirsready");
        read_load_order_txt_from_gametype();
        call_deferred("load_all_load_order_mods");

    }

    public void create_default_configs()
    {
        //ASK EACH GENERATOR TO MAKE A DEFAULT CONFIG BEFORE WE SIGNAL MODSREADY TO MAKE SURE THEY'RE NOT LOADED TOO EARLY
        get_parent().get_node("DSL_WhitelistLoot").write_default_whitelistloot_config();
        get_parent().get_node("DSL_ItemlotEnemy").write_default_itemlotid_blacklist();
        get_parent().get_node("DSL_SpEffectHandler").copy_default_speffect_data(get_modfolder_path(6) + "/SpEffectParam.csv", get_modfolder_path(6) + "/SpEffectConfig_Default.csv");
        copy_file("res://DefaultData/ER/CSVs/EquipWeaponParam.csv", get_modfolder_path(MF.P_Weapons) + "/EquipWeaponParam.csv", false);
        copy_file("res://DefaultData/ER/EquipWeaponParam_Shields.txt", get_modfolder_path(MF.P_Weapons) + "/EquipWeaponParamShields.csv", false);
        copy_file("res://DefaultData/ER/CSVs/StaffsSeals/EquipParamWeapon_StaffsSeals.csv", get_modfolder_path(MF.P_Weapons) + "/EquipParamWeapon_StaffsSeals.csv", false);
        //FOR SOME REASON THIS IS COMING BACK CORRUPTED, LEAVING IT OUT FOR NOW
        copy_file("res://DefaultData/ER/CSVs/EquipParamProtector.csv", get_modfolder_path(MF.P_Armor) + "/EquipParamProtector.csv", false);
        copy_file("res://DefaultData/ER/CSVs/RaritySetup.csv", get_modfolder_path(MF.Rarities) + "/RaritySetup.csv", false);

    }

    public void signal_x_ready(String signalname)
    {
        call_deferred("emit_signal", signalname);
        //print_debug("ModManager signalling "+signalname)

    }

    public void load_all_load_order_mods()
    {
        foreach (var x in LoadOrder.size())
        {
            load_mods(LoadOrder[x]);
        }
        signal_x_ready("modsready");
        call_deferred("validate_extra_parts_folders");

    }

    public void read_load_order_txt_from_gametype()
    {
        String game = get_gameid();
        String loadorderpath = ExePath + "/" + DataFolder + "/" + game;
        //CREATE AN ARRAY OF VALID MOD FOLDERS - THIS IS WHAT WE'LL SET AS THE LOADORDER VARIABLE ONCE WE'VE CHECKED THEY EXIST
        Array validmodfolders = new Array() { };
        //CREATE A SECOND ARRAY TO STORE EVERY MOD FOLDER LISTED IN THE LOAD ORDER - THIS WILL STORE ALL THE MOD FOLDERS LISTED IN THE LOAD ORDER FOR CHECKING AFTERWARD
        Array listedmodfolders = new Array() { };
        //OPEN LOADORDER.TXT AND ADD ITS ENTRIES TO LISTEDMODFOLDERS
        var lo = FileAccess.open(loadorderpath + "/loadorder.txt", FileAccess.READ);
        while (!lo.eof_reached())
        {
            var newloentry = lo.get_line();
            if (newloentry != "")
            {
                //SKIP LOADING IF THE LINE CONTAINS A HASH
                if (!newloentry.contains("#"))
                {
                    listedmodfolders.append(newloentry);
                    //NOW WE'VE GOT THE LISTED MODS, CHECK IF THE DIRECTORIES ACTUALLY EXIST AND ADD THEM TO VALIDMODFOLDERS IF THEY DO
                }
            }
        }
        foreach (var x in listedmodfolders.size())
        {
            var lodir = DirAccess.dir_exists_absolute(get_mod_path(listedmodfolders[x]));
            if (lodir)
            {
                validmodfolders.append(listedmodfolders[x]);
                //print_debug(listedmodfolders[x]+" seems valid, adding to load order...")
                //THROW AN ERROR IF NO MODS WERE FOUND TO BE VALID AND CLOSE DSL AS IT WON'T WORK WITHOUT ANY DATA SETS
                //print_debug(GD.Str(validmodfolders)+" Valid / "+str(listedmodfolders)+" Listed")
            }
        }
        if (validmodfolders.size() == 0)
        {
            OS.alert("No valid mod/data set folders in " + loadorderpath + "/loadorder.txt! DSL will !work without any data sets!\n\nPlease check the contents of the loadorder.txt file && restart DSL.");
            get_tree().quit();
        }
        else
        {
            //OTHERWISE ADD ALL THE VALID MOD FOLDERS TO LOAD ORDER
            foreach (var x in validmodfolders.size())
            {
                LoadOrder.append(validmodfolders[x]);
                //print_debug("Load Order: "+str(LoadOrder))

            }
        }
    }

    public void validate_extra_parts_folders()
    {
        //CHECK EACH VALIDATED LOAD ORDER MOD'S EXTRAPARTS FOLDER, IF IT'S NOT EMPTY QUEUE IT UP FOR COPYING
        foreach (var x in LoadOrder)
        {
            String extrapartspath = get_modfolder_path(MF.ExtraParts, x);
            //print_debug("EXTRA PARTS PATH: "+str(extrapartspath))
            //DO NOTHING IF THE DIRECTORY DOESN'T EXIST
            if (DirAccess.dir_exists_absolute(extrapartspath))
            {
                //COLLECT THE FILES IN THIS DIRECTORY, IF IT'S NOT EMPTY ADD THIS EXTRA PARTS DIRECTORY TO EXTRAPARTS
                Array files = DirAccess.get_files_at(extrapartspath);
                print_debug(files);
                if (!files.is_empty())
                {
                    ExtraParts.append(extrapartspath);
                    print_debug(extrapartspath + " ADDED TO EXTRA PARTS ARRAY");
                }
            }
            else
            {
                print_debug("EXTRA PARTS FOLDER " + extrapartspath + " NOT FOUND, SKIPPING...");
            }
        }
        print_debug("EXTRA PARTS: " + str(ExtraParts));

    }

    public String create_extra_parts_bat_text()
    {
        //DO NOTHING IF WE DON'T HAVE ANY EXTRAPARTS DETECTED
        if (ExtraParts.is_empty())
        {
            return "";
        }
        String finalbat = "";
        //START BY GETTING THE DESTINATION TOGETHER - CREATE THE PARTS FOLDER DIRECTORY
        finalbat += "cd /d \"%modfolder%\"\nset \"partsfolder=%cd%\\parts\"\n";
        const partsfolder : String = "\"%partsfolder%\"";
        const batstart : String = "xcopy /I ";
        //ITERATE OVER ALL EXTRAPARTS FOLDERS AND CREATE 
        foreach (var x in ExtraParts)
        {
            finalbat += batstart + "\"" + x + "\" " + partsfolder;

        }
        return finalbat;

    }

    public void load_mods(String modname = "Default")
    {
        //TAKING MODNAME, WE CHECK IF IT HAS EACH MODFOLDERS DIRECTORY AVAILABLE, IF SO CHECK IF IT'S GOT ANYTHING IN IT, AND PASS THE FILES INSIDE
        //TO LOADEDMODS FOR THE INDIVIDUAL GENERATORS TO VALIDATE AND LOAD
        String gameid = get_node("/root/GlobalVariables").GameID;
        String mfpath = ExePath + "/" + DataFolder + "/" + gameid + "/" + modname;
        if (DirAccess.dir_exists_absolute(mfpath))
        {
            //print_debug(GD.Str(mfpath)+" exists, checking for ModFolders...")
            //NOW WE NOW DIR EXISTS, CHECK THE DIRECTORY FOR EACH MODFOLDERS FOLDER, IF IT EXISTS ADD IT TO LOADEDMODS, GET ITS CONTENTS AND ADD TO THE
            //ARRAY OF EACH MODFOLDER
            foreach (var x in ModFolders.size())
            {
                //SKIP WEAPONS FOLDER AS WE'LL BE HANDLING THIS DIFFERENTLY
                //^ DUMMIED OUT AS OF 09/04/23 - WE WILL BE DOING THIS NORMALLY AND FIND ANOTHER WAY TO PARSE WEAPONTYPES
                //if x != MF.P_Weapons:
                String currentmfinpath = mfpath + "/" + ModFolders[x];
                if (DirAccess.dir_exists_absolute(currentmfinpath))
                {
                    //IF LOADEDMODS DOES NOT HAVE MODNAME, INITIALISE A NEW DICTIONARY FOR THE MODFOLDER'S FILES
                    if (!LoadedMods.has(modname))
                    {
                        LoadedMods[modname] = new Dictionary() { };
                        //INITIALISE ARRAY OF THE FILES IN THE CURRENT MODFOLDER
                    }
                    var filesinhere = DirAccess.get_files_at(currentmfinpath);
                    //ADD THE FULL FILEPATH TO EACH OF THESE ENTRIES BEFORE WE ADD THEM
                    Array finalfilesinhere = add_full_path_to_loadmods_array(currentmfinpath, filesinhere);
                    //ADD ARRAY TO LOADEDMODS' ENTRY FOR THE CURRENT MOD - EVEN IF IT'S EMPTY THIS SHOULD HELP US AVOID CRASHES IF WE REFER TO
                    //A MOD THAT DOESN'T HAVE A PARTICULAR FOLDER
                    LoadedMods[modname][ModFolders[x]] = new Array() { };
                    LoadedMods[modname][ModFolders[x]] = finalfilesinhere;
                }
                else
                {
                    print_debug("Mod " + modname + " doesn"t have \"+ModFolders[x]+\" folder, skipping...\");
            //CHECK RESULTS
            }
            }
            print_debug(LoadedMods);
        }
        else
        {
            OS.alert(\"Cannot find mod folder \"+modname+\" at \"+mfpath+\", skipping...\");

    //DEFAULT CREATION FUCNTIONS
        }
    }

    public void create_default_mod_directory()
    {
        //NOW ITERATE OVER THE MODFOLDERS ARRAY, USING \"DEFAULT\" AS THE MOD"S NAME - WE'LL RELY ON THE CREATE_DIRECTORY FUNCTION'S RECURSIVELY
        //CREATING NATURE TO MAKE THIS POSSIBLE
        create_directory_set(ModFolders, get_current_gameid_path() + "/Default");
        //CREATE WEAPONFOLDERS - DUMMYING OUT AS OF 09/04/23
        //create_directory_set(WeaponFolders,get_current_gameid_path()+"/Default/"+ModFolders[MF.P_Weapons])
        create_default_loadorder_txt(get_current_gameid_path());

    }

    public void create_output_folder_with_gametype()
    {
        //DISABLED AS OF ALPHA 3 - LOOT NOW EXPORTS TO MODFOLDER
        //create_directory(get_export_path(true))

    }

    public void create_default_loadorder_txt(String path = "")
    {
        if (DirAccess.dir_exists_absolute(path))
        {
            //IF PATH IS VALID, CHECK IF LOADORDER.TXT EXISTS AND IF IT DOESN'T, CREATE IT WITH THE "DEFAULT" MOD LOADED
            if (!FileAccess.file_exists(path + "/loadorder.txt"))
            {
                var newloadorder = FileAccess.open(path + "/loadorder.txt", FileAccess.WRITE);
                newloadorder.store_string("Default")
                //print_debug("Creating Default LoadOrder.txt")

                //INFORMATION FUNCTIONS

        }
        }
    }

    public String get_load_order_path_from_gametype()
    {
        //CREATE DEFAULT LOADORDER.TXT IF WE DON'T ALREADY HAVE ONE IN CURRENTGAMEIDPATH
        if (!FileAccess.file_exists(get_current_gameid_path() + "/loadorder.txt"))
        {
            create_default_loadorder_txt(create_default_loadorder_txt(get_current_gameid_path()));
        }
        return get_current_gameid_path() + "/loadorder.txt";

    }

    public void get_valid_modfolders(bool withgamepath = false)
    {
        String firstpath = withgamepath ? get_current_gameid_path() +{
            "/", ""


}
    }

    public String get_modfolder_path(int which = 0, String whichmod = "Default")
    {
        var finalwhich = which < ModFolders.size() ? which : 0

    return get_current_gameid_path() + "/" + whichmod + "/" + ModFolders[finalwhich];

    }

    public String get_mod_path(String which = "Default")
    {
        return get_current_gameid_path() + "/" + which;

    }

    public Array add_full_path_to_loadmods_array(String pathtoadd, Array arraytochange)
    {
        Array finalarray = new Array() { };
        foreach (var x in arraytochange.size())
        {
            finalarray.append(pathtoadd + "/" + arraytochange[x]);
        }
        return finalarray;
    }

    public Dictionary get_all_files_in_modfolder_from_all_mods(int modfolderid = 0, Array bannedcontains = new Array() { })
    {
        //THIS FUNCTION GRABS ALL THE MATCHING LOADEDMODS ENTRIES AND COMPILES THEM INTO A SINGLE ARRAY FOR QUICK LOADING
        //BY ITS RELEVANT GENERATOR
        var dicttoreturn : Dictionary = new Dictionary(){
            {"result",new Array(){}},
        {"notdefault",new Array(){}
    }};
        foreach (var x in LoadedMods.keys().size())
        {
            String currentmod = LoadedMods.keys()[x];
            //MARK FILES AS PRIORITY IF THEY'RE MODDED - I.E. NOT DEFAULT
            bool priority = currentmod != "Default";
            //CHECK IF MOD HAS MODFOLDER
            if (LoadedMods[currentmod].has(ModFolders[modfolderid]))
            {
                if (is_array_size_more_than(LoadedMods[currentmod][ModFolders[modfolderid]], 0, false))
                {
                    dicttoreturn["result"].append_array(LoadedMods[currentmod][ModFolders[modfolderid]])

                if (priority)
                    {
                        dicttoreturn["notdefault"].append_array(LoadedMods[currentmod][ModFolders[modfolderid]])
    //FILTER OUT ANY FILES CONTAINING BANNED CHARACTERS
                }
                }
            }
        }
        if (!bannedcontains.is_empty())
        {
            print_debug("FILTERING " + str(ModFolders[modfolderid]) + " GAFINMFFAM Result!");
            Array newfinalarray = new Array() { };
            foreach (var x in dicttoreturn["result"])
            {
                bool valid = true;
                foreach (var y in bannedcontains)
                {
                    //print_debug("Checking GAFINMFFAM "+str(y))
                    if (GD.Str(x).contains(GD.Str(y)))
                    {
                        //print_debug("FOUND GAF "+str(y)+", "+str(x)+" INVALID!")
                        valid = false;
                    }
                }
                if (valid)
                {
                    newfinalarray.append(x)

            }
            }
            print_debug("FILTERED GAFINMFFAM: " + str(newfinalarray));
            dicttoreturn["result"] = newfinalarray
        //print_debug(GD.Str(ModFolders[modfolderid])+" GAFINMFFAM Result: "+str(dicttoreturn))
    }
        return dicttoreturn



}


}
*/