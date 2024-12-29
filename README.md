DSLRNet
=======

DSLRNet is a dotnet rewrite of the original [Diablo Style Loot Remastered (Alpha 3.1)](https://www.nexusmods.com/eldenring/mods/3498) by CornflakeRush.  The original mod did not work post DLC and I needed it for an overhaul I was creating for friends.  Since I went through the work of making the app to generate this stuff, I wanted to give back and release the app as a usable thing with a GUI.

A goal was to keep compatibility with how the original mod could be configured and this was mostly achieved.  The Assets folder of the DSLRNet.Core project should have some very familiar things to it.

Features
--------

### Loot Generation

Allows controlling how many item lots are generated per enemy/map/chest/boss separately.  For enemies that are farmable you can change how many possible items are generated per lot.

The way this works is the app will scan the game files to find all enemies, map pickups, chest opens and event driven boss drops.  Then based on the average scaling SpEffects are applied to enemies of the map the "GameStage" of the drops are determined.  This drives what rarity the items drop at.

### Custom Icons

This app allows automatic generation and injection of custom icon backgrounds for the diablo style loot generated items.  It does this by injecting icon sheets into the menu/hi/01_common.tpf.dcx file and updating the menu/hi/01_common.sblytbnd.dcx file to include texture atlases for the new icon ids.  NOTE: hi def icons cannot be supported due to a massive bloat in memory and game instability. While technically possible, a whole different approach to doing these icons (maybe change gfx files for inventory to allow an item layer below the icon so it's not icon sheets per rarity group?)

In the app you can change which background to use for which group of rarities. The groupings and what file name to use are controlled within the [Settings.ini](src/Product/DSLRNet.Core/Settings.ini) file.

Item Lot Example
----------------

When an enemy is defeated, some events which reward items are triggered, a chest is opened or an item in the open world map is picked up it is assigned an "ItemLot" to roll against. Each ItemLot can contain up to 8 items, each with a value assigned from 0 to 1000. The sum of all 8 items should always be 1000. A random value between 0 and 1000 is rolled, and the value is iterated through each item until the sum of all items checked is greater than the rolled value.

Additionally, when an ItemLot is rolled, if there is another ItemLot that is +1 sequentially by ID, that ItemLot is also evaluated. An item may be awarded for all sequential ItemLots in a row.

### Example Table

| ItemLot ID | Item 1 | Item 2 | Item 3 | Item 4 | Item 5 | Item 6 | Item 7 | Item 8 | Sum  |
|------------|--------|--------|--------|--------|--------|--------|--------|--------|------|
| 1000       | 200    | 300    | 100    | 100    | 100    | 100    | 50     | 50     | 1000 |
| 1001       | 500    | 200    | 100    | 100    | 50     | 25     | 25     | 0      | 1000 |
| 1002       | 250    | 250    | 250    | 100    | 100    | 50     | 0      | 0      | 1000 |

### How It Works

1. **ItemLot 1000**:
    - Items: [200, 300, 100, 100, 100, 100, 50, 50]
    - Sum: 1000
    - If a random value of 450 is rolled:
        - Item 1 (200) + Item 2 (300) = 500 (greater than 450)
        - Item 2 is awarded.

2. **ItemLot 1001**:
    - Items: [500, 200, 100, 100, 50, 25, 25, 0]
    - Sum: 1000
    - If a random value of 750 is rolled:
        - Item 1 (500) + Item 2 (200) + Item 3 (100) = 800 (greater than 750)
        - Item 3 is awarded.

3. **ItemLot 1002**:
    - Items: [250, 250, 250, 100, 100, 50, 0, 0]
    - Sum: 1000
    - If a random value of 600 is rolled:
        - Item 1 (250) + Item 2 (250) + Item 3 (250) = 750 (greater than 600)
        - Item 3 is awarded.

Contributing
------------

If you have any ideas, suggestions, or bug reports, please open an issue or submit a pull request.

Credits
-------

- This project is an extension/rewrite of the [Diablo Style Loot Remastered (Alpha 3.1)](https://www.nexusmods.com/eldenring/mods/3498) by CornflakeRush.  Some code inspriation was taken from that mod and integrated here.
- PARAM Defs borrowed from the [Smithbox](https://github.com/vawser/Smithbox/tree/main/src/StudioCore/Assets/PARAM/ER) project headed by vawser to allow direct read/writes of various regulation bin params.
- The [SoulsFormats](https://github.com/JKAnderson/SoulsFormats) project to enable interactions with souls games files.
- Extra special thanks to the fine people at the ?ServerName? discord server and the [souls modding wiki](http://soulsmodding.wikidot.com/tutorial:main) for having a searchable wealth of knowledge on how this stuff works

License
-------

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
    
