namespace DSLRNet.Core.Extensions;

using System.Collections.Generic;
using System.Linq;

public static class MSBEExtensions
{
    public static List<NpcParam> FilterRelevantNpcs(this MSBE msb, ILogger logger, Dictionary<int, NpcParam> allNpcs, string mapFileName)
    {
        List<NpcParam> npcs = [];

        foreach (var enemy in msb.Parts.Enemies)
        {
            int modelNumber = int.Parse(enemy.ModelName.Substring(1));

            // Range is to ignore wildlife drops
            if (modelNumber >= 2000 && modelNumber <= 6000 || modelNumber >= 6200)
            {
                if (!allNpcs.TryGetValue(enemy.NPCParamID, out NpcParam? item))
                {
                    logger.LogDebug($"NPC with ID {enemy.NPCParamID} from map {mapFileName} with model {modelNumber} did not match a param");
                    continue;
                }
                else if (item.itemLotId_enemy < 0 && item.itemLotId_map < 0)
                {
                    logger.LogDebug($"NPC with ID {enemy.NPCParamID} from map {mapFileName} with model {modelNumber} did not have an item lot associated with it");
                    continue;
                }

                if (!npcs.Any(d => d.ID == enemy.NPCParamID))
                {
                    npcs.Add(item);
                }
            }
        }

        return npcs;
    }
}
