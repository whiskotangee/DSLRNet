# USING ANY MODS WITH ELDEN RING RISKS BEING BANNED FROM THE ONLINE MODE!
**PLEASE MAKE SURE TO BACK UP YOUR SAVE FILES AND EITHER [PLAY USING SEAMLESS CO-OP AND MODENGINE TOGETHER](https://discord.com/channels/979042878091329587/1075129921493540926) (EVEN SINGLE PLAYER) OR PLAY OFFLINE ONLY! USING ANY MODS ON VANILLA SAVES THAT END UP GOING ONLINE RISKS A BAN!**

**Please note that this mod requires SOTE installed/enabled as it was written assuming the expansion is available for the user playing it.**
**It is highly recommended that you start a new playthrough for this mod.  All of the map and boss drops share flags with the original drops so if you have already killed a boss, opened a chest or 

## Overview

This mod is a re-write of the [Diablo Style Loot Remastered (Alpha 3.1) at Elden Ring Nexus - Mods and Community](https://www.nexusmods.com/eldenring/mods/3498) mod that was seemingly abandoned before SOTE released. As part of a meme overhaul that was made for friends, I created a re-write of the mod that worked with SOTE and added more bells and whistles to it. The application should be much more configurable, portable and hopefully future proof, at least in a way where code changes should be minimal to update.

### New Features
* Default rarity ranges (Uncommon, Rare, Mythical, Legendary) and Unique weapons get custom icon backgrounds in the inventory. The app lets you configure these to any images you want. These files are automatically injected to the correct mod files when the app is run.
* Automated scanning of game files pre-bakes all enemy, map, chest and boss drops and then correlates them to a game stage (Early, Mid, Late, End.) The default configuration gives a nice power creep that lasts the whole game. Limgrave will drop Uncommon items, but by the time you start getting into Caelid/Altus Plateau you will start seeing Rares. By the time you get to the end game Mythical and Legendary items will start dropping.
* Expanded lore generation using a simple mad libs randomized text masher. The default is setup such that while it doesn't always make sense it is for the most part vanilla lore friendly. The files that control the lore generation are found in the "Assets\Lore" folder of the app. If you would like a much more brainrot/meme edition, copy the files in the "Assets\BrainRotEdition" into the "Assets" folder and overwrite the existing RaritySetup.csv and Lore files.

For those curious to dive in more or get inspired for their own projects, the source is here [DSLRNet v1.0 on GitHub](https://github.com/whiskotangee/DSLRNet/tree/v1.0)

### Usage Warnings

*It is highly recommended that you start a new playthrough for this mod.  All of the map and boss drops share flags with the original drops so if you have already killed a boss, opened a chest or picked up something on the map, those items will never be obtainable for you
* Re-running the tool with the exact same settings should produce the same results. Re-running this tool after changing pretty much any of the settings will result in significant changes that will make generated equipment disappear/change.
* If wanting to share with friends in a seamless co-op playthrough, either run the tool with the same settings on the same version of the game, or share the generated files with the other person.

## Installation:

### Pre-requisites:
* ModEngine2 [Releases · soulsmods/ModEngine2](https://github.com/soulsmods/ModEngine2/releases)
* Dotnet 8 Desktop Runtime (https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.11-windows-x64-installer)
* Patch 1.16 introduced a size limit on regulation.bin files. Since this adds so many items to the game the limit is easily broken and save corrupted errors will occur. To avoid that use either [Elden Ring Alt Saves at Elden Ring Nexus - Mods and Community](https://www.nexusmods.com/eldenring/mods/2651) or [regulationbinmeme.dll](https://discord.com/channels/529802828278005773/529900741998149643/1296918915762487367) from discord ?ServerName? > tools-and-resources > regbinmeme.dll.

### Step 1: Download and Extract The Mod
1. Download the latest version of this mod.
2. Extract the "diabloloot_er" folder from the zip file anywhere that is convenient.
3. Ensure your ME2 root folder contains the following directories:
   - "mod"
   - "modengine2" (lowercase, this holds all of ME2's internal files)
   - "DSLRNet"

### Step 2: Modify Config File
1. Open "config_eldenring.toml".
2. Scroll to the bottom and locate the following line:
```toml
    mods = [
      { enabled = true, name = "mods", path = "mod" }
    ]
```
   This list controls the mod folders ME2 loads into Elden Ring. Higher entries take priority over lower ones.

### Step 3: Add the Mod To the List
1. Place your cursor at the opening square bracket `{` before "enabled" and press Enter.
2. Copy and paste the existing line above itself and modify it as follows:
   - Change `name = "mods"` to `name = "DSLRNet"`.
   - Change `path = "mod"` to `path = "DSLRNet"`.
   - Add a comma at the end of the new line.

   The updated section should look like this:
```toml
    mods = [
      { enabled = true, name = "DSLRNet", path = "DSLRNet" },
      { enabled = true, name = "mods", path = "mod" }
    ]
```

### Step 4: Launch DSLRNet.exe
1. Locate DSLRNet.exe in the folder you expanded this mod into.
2. Run the exe.
3. Set the game folder in the Game Path box (Or click Browse to browse to eldenring.exe and select it).
4. Set the deploy folder to the DSLRNet folder created above.
5. IF you have many other mod folders and want this mod to merge with them when it's run, click the PARSE MODENGINE TOML button and choose the config_eldenring.toml file. This will set up the file priority list the same as modengine2 has it. The app will then read the regulation.bin from the highest priority it can find the file in.
6. Change any other settings you would like in the different configuration tabs available.
7. Click GENERATE LOOT and allow the app to do its job. When finished, you will have a little message box appear.

### Step 5: Launch the Game and have fun!
1. Run "launchmod_eldenring.bat".
2. If everything is set up correctly, Elden Ring will load with both Diablo Style Loot and any mods in the "mod" directory.

## Advanced users - Rescanning with other mods

The app allows for re-scanning game files to determine the item lot distribution by type and difficulty. This uses the same priority system and settings the UI app does so if you configure the mod order there with the PARSE MODENGINE TOML feature then a rescan will consider mod files before game files. If another mod changes around itemlots a whole bunch then this rescan should pick that up and assign loot accordingly.

To run the rescan:
* Ensure [UXM Selective Unpacker at Elden Ring Nexus - Mods and Community](https://www.nexusmods.com/eldenring/mods/1651) has been run and the game has all files unpacked.
* Ensure the Settings.User.Ini file is updated in the same directory with the right OrderedModFolders/GamePath/DeployPath set.
* Launch `DSLRNet.Console.exe --rescan`.

After this rescan, the baked files (Located in the Assets/Data/ItemLots/Scanned folder) will be updated with the itemlots and what kind of game stage they should be evaluated to according to difficulty. Simply run the DSLRNet.exe GUI app and Generate Loot again to apply the rescanned itemlot changes.

## Credits
* This project is a rewrite of the [Diablo Style Loot Remastered (Alpha 3.1)](https://www.nexusmods.com/eldenring/mods/3498) by CornflakeRush. Some code inspiration was taken from that mod and integrated here.
* PARAM Defs borrowed from the [Smithbox](https://github.com/vawser/Smithbox/tree/main/src/StudioCore/Assets/PARAM/ER) project headed by vawser to allow direct read/writes of various regulation bin params.
* The [SoulsFormats](https://github.com/JKAnderson/SoulsFormats) project to enable interactions with souls games files.
* Extra special thanks to the fine people at the ?ServerName? discord server and the [souls modding wiki](http://soulsmodding.wikidot.com/tutorial:main) for having a searchable wealth of knowledge on how this stuff works.

Now, it should be all set! If you need any further modifications or adjustments, let me know.