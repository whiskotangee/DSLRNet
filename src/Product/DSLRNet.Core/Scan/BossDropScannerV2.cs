namespace DSLRNet.Core.Scan;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;

using static SoulsFormats.EMEVD.Instruction;

public class BossDropScannerV2(ILogger<BossDropScannerV2> logger, FileSourceHandler fileHandler, DataAccess dataAccess)
{
    private Dictionary<long, CommonBossEventConfig> bossDeathFunctions = [];
    private Dictionary<long, CommonBossEventConfig> itemRewardingFunctions = [];
    private Dictionary<long, long> flagToItemLotMapping = [];

    public List<EventDropItemLotDetails> ScanEventsForBossDrops()
    {
        // start with GameAreaParam, these are boss entity ids

        Dictionary<uint, int> bossDeathFlags = [];
        foreach (GameAreaParam gameAreaParam in dataAccess.GameAreaParam.GetAll())
        {
            if (gameAreaParam.defeatBossFlagId > 0)
            {
                bossDeathFlags.TryAdd(gameAreaParam.defeatBossFlagId, gameAreaParam.ID);
            }
        }

        // scan common emevds for flags that give item lots

        this.bossDeathFunctions = [];
        this.itemRewardingFunctions = [];
        this.flagToItemLotMapping = [];

        ScanFunctionDefinitions(EMEVD.Read(GetCommonEmevdFile("common_func.emevd.dcx")), "common_func");

        List<EventDropItemLotDetails> lotDetails = [];
        string commonEmevdFile = GetCommonEmevdFile("common.emevd.dcx");

        EMEVD emevd = EMEVD.Read(commonEmevdFile);
        ScanFunctionDefinitions(emevd, Path.GetFileNameWithoutExtension(commonEmevdFile));
        ScanMapEvents(emevd, Path.GetFileNameWithoutExtension(commonEmevdFile), lotDetails);

        // scan all other emevds, look for boss defeat events, then for setting flags that trigger item lots

        //var otherEmveds = fileHandler.ListFilesFromAllModDirectories("event", "m60_38_41*emevd.dcx");
        List<string> otherEmveds = fileHandler.ListFilesFromAllModDirectories("event", "m*emevd.dcx");
        foreach (string? mapEventFile in otherEmveds.Distinct())
        {
            EMEVD mapEmevd = EMEVD.Read(mapEventFile);
            string mapId = GetMapId(mapEventFile);
            logger.LogInformation($"Scanning events from {mapId} for boss drops");
            ScanFunctionDefinitions(mapEmevd, mapId);
            ScanMapEvents(mapEmevd, mapId, lotDetails);
        }

        Dictionary<uint, int> filteredBossFlags = bossDeathFlags.Where(d => !lotDetails.Any(s => (s.EventTriggerFlagId == d.Key || s.EntityId == d.Key) && s.ItemLotId > 0)).ToDictionary(d => d.Key, d => d.Value);

        logger.LogDebug($"Bosses missing event drops {filteredBossFlags.Count} - {string.Join(",", filteredBossFlags.Select(d => d.Key))}");
        logger.LogInformation($"Found {lotDetails.Where(d => d.ItemLotId > 0).DistinctBy(d => d.ItemLotId).Count()} boss drop events");

        Dictionary<string, IEnumerable<EventDropItemLotDetails>> res = EventDropItemLotDetails.SummarizeUnsetProperties(lotDetails);
        foreach (KeyValuePair<string, IEnumerable<EventDropItemLotDetails>> toLog in res)
        {
            logger.LogInformation($"{toLog.Key} - {string.Join(Environment.NewLine, toLog.Value)}");
        }

        return [.. lotDetails];
    }

    private string GetCommonEmevdFile(string name)
    {
        if (!fileHandler.TryGetFile(Path.Combine("event", name), out string commonEmevdFile))
        {
            throw new Exception($"Could not find {name}");
        }

        return commonEmevdFile;
    }

    private string GetMapId(string mapEventFile)
    {
        string mapId = Path.GetFileName(mapEventFile);
        return mapId[..mapId.IndexOf('.')];
    }

    private EventDropItemLotDetails? EvaluateInitializeEventInstruction(string mapName, EMEVD.Instruction instruction, List<long> args)
    {
        if (!instruction.IsInitializeEvent())
        {
            return null;
        }

        long eventId = args[1];

        if (this.bossDeathFunctions.TryGetValue(eventId, out CommonBossEventConfig? value))
        {
            long itemLotId = 0;

            if (value.HardCodedFlags.Count != 0)
            {
                foreach (long flag in value.HardCodedFlags)
                {
                    if (this.flagToItemLotMapping.TryGetValue(flag, out long flagBasedItemLotId))
                    {
                        itemLotId = flagBasedItemLotId;
                    }
                }
            }

            if (value.HardCodedItemLots.Count != 0)
            {
                itemLotId = value.HardCodedItemLots.First();
            }
            else 
            {
                if (itemLotId == 0)
                {
                    itemLotId = value.ItemLotIdIndexes.Count != 0 && value.ItemLotIdIndexes.First() < args.Count ? args[value.ItemLotIdIndexes.First()] : value.HardCodedItemLots.FirstOrDefault();
                }
            }

            return new EventDropItemLotDetails()
            {
                EntityId = value.EntityIdIndexes.Count != 0 && value.EntityIdIndexes.First() < args.Count ? (int)args[value.EntityIdIndexes.First()] : (int)value.HardCodedEntityId,
                MapId = mapName,
                EventTriggerFlagId = value.FlagIndexes.Count != 0 && value.FlagIndexes.First() < args.Count ? (int)args[value.FlagIndexes.First()] : (int)value.HardCodedFlags.First(),
                ItemLotId = (int)itemLotId,
            };
        }
        else if(this.itemRewardingFunctions.TryGetValue(eventId, out value))
        {
            if (value.HardCodedFlags.Count != 0 || value.FlagIndexes.Any(d => d >= 0))
            {
                int flag = value.FlagIndexes.Count != 0 && value.FlagIndexes.First() < args.Count ? (int)args[value.FlagIndexes.First()] : (int)value.HardCodedFlags.First();
                long itemLotId = value.ItemLotIdIndexes.Count != 0 && value.ItemLotIdIndexes.First() < args.Count ? args[value.ItemLotIdIndexes.First()] : value.HardCodedItemLots.First();

                if (itemLotId > 0)
                {
                    flagToItemLotMapping[flag] = itemLotId;
                }
            }
        }

        return null;
    }

    private void ScanFunctionDefinitions(EMEVD mapEmevd, string mapId)
    {
        logger.LogInformation($"Compiling list of relevant functions from {mapId}");

        foreach (EMEVD.Event? ev in mapEmevd.Events)
        {
            bool isBossDeathFunction = ev.Instructions.Any(d => d.IsProcessHandleBossDefeatAndDisplayBanner());
            bool isItemRewardingFunction = ev.Instructions.Any(d => d.IsProcessAwardItemsIncludingClients());

            if (!isBossDeathFunction && !isItemRewardingFunction)
            {
                continue;
            }

            CommonBossEventConfig? config = null;

            if (isBossDeathFunction)
            {
                if (!this.bossDeathFunctions.TryGetValue(ev.ID, out CommonBossEventConfig? bossDeathFunction))
                {
                    bossDeathFunction = new CommonBossEventConfig() { EventId = ev.ID };
                    this.bossDeathFunctions.Add(ev.ID, bossDeathFunction);
                }
                config = bossDeathFunction;
            }
            else if (isItemRewardingFunction)
            {
                if (!this.itemRewardingFunctions.TryGetValue(ev.ID, out CommonBossEventConfig? itemRewardingFunction))
                {
                    itemRewardingFunction = new CommonBossEventConfig() { EventId = ev.ID };
                    this.itemRewardingFunctions.Add(ev.ID, itemRewardingFunction);
                }
                config = itemRewardingFunction;
            }
            else
            {
                throw new InvalidOperationException("Should not be able to get here");
            }

            // otherwise this is an event that processes boss defeat, we want to figure out if this event is called what params are for what, also what flags are set in this event

            for (int i = 0; i < ev.Instructions.Count; i++)
            {
                EMEVD.Instruction instruction = ev.Instructions[i];

                int instructionCount = instruction.ArgData.Length / 4;
                List<long> args = instruction.UnpackArgs(Enumerable.Repeat(ArgType.Int32, instructionCount)).Select(Convert.ToInt64).ToList();

                List<EMEVD.Parameter> parameters = ev.Parameters.Where(d => d.InstructionIndex == i).ToList();

                if (instruction.IsProcessHandleBossDefeatAndDisplayBanner())
                {
                    // entity Id parameter index is based on this
                    long entityId = args[0];
                    if (entityId > 0)
                    {
                        config.HardCodedEntityId = entityId;
                    }

                    if (parameters.Count > 0)
                    {
                        config.EntityIdIndexes.Add(2 + (int)parameters[0].SourceStartByte / 4);
                    }
                }
                else if (instruction.IsProcessSetEventFlagID())
                {
                    long flagId = args[1];
                    if (flagId > 0)
                    {
                        config.HardCodedFlags.Add(flagId);
                    }

                    if (parameters.Count > 0)
                    {
                        config.FlagIndexes.Add(2 + (int)parameters[0].SourceStartByte / 4);
                    }
                }
                else if (instruction.IsProcessAwardItemsIncludingClients()
                    || instruction.IsProcessAwardItemLot())
                {
                    long itemLotId = args[0];
                    if (itemLotId > 0)
                    {
                        config.HardCodedItemLots.Add(itemLotId);
                    }

                    if (parameters.Count > 0)
                    {
                        config.ItemLotIdIndexes.Add(2 + (int)parameters[0].SourceStartByte / 4);
                    }
                }
                else if (instruction.IsProcessUnknown200476())
                {
                    long flagId = args[0];
                    long itemLotId = args[1];

                    if (flagId > 0)
                    {
                        config.HardCodedFlags.Add(flagId);
                    }
                    else
                    {
                        config.FlagIndexes.Add(2 + (int)parameters[0].SourceStartByte / 4);
                    }

                    if (itemLotId > 0)
                    {
                        config.HardCodedItemLots.Add(itemLotId);
                    }

                    if (parameters.Count > 0)
                    {
                        config.ItemLotIdIndexes.Add(2 + (int)parameters[1].SourceStartByte / 4);
                    }
                }
            }
        }
    }

    private void ScanMapEvents(EMEVD mapEmevd, string mapId, List<EventDropItemLotDetails> lotDetailsList)
    {
        logger.LogInformation($"Scanning events from {mapId}"); 

        foreach (EMEVD.Event? ev in mapEmevd.Events)
        {
            EventDropItemLotDetails lotDetails = new();

            foreach (EMEVD.Instruction? instruction in ev.Instructions)
            {
                int instructionCount = instruction.ArgData.Length / 4;
                List<long> args = instruction.UnpackArgs(Enumerable.Repeat(ArgType.Int32, instructionCount)).Select(Convert.ToInt64).ToList();

                string str = string.Join(',', args);

                // common event happens, find it in boss death, if boss death has hard coded flag then look up flag in item functions

                EventDropItemLotDetails? itemLotDetails = EvaluateInitializeEventInstruction(mapId, instruction, args);
                if (itemLotDetails != null)
                {
                    AddOrUpdate(itemLotDetails, lotDetailsList);
                }
            }
        }
    }

    private void AddOrUpdate(EventDropItemLotDetails lotDetails, List<EventDropItemLotDetails> lotDetailsList)
    {
        List<EventDropItemLotDetails> existing = [];

        if (lotDetails.EventTriggerFlagId > 0)
        {
            existing = lotDetailsList.Where(d => d.EventTriggerFlagId == lotDetails.EventTriggerFlagId).ToList();
        }

        if (existing.Count == 0 && lotDetails.EntityId > 0)
        {
            existing = lotDetailsList.Where(d => d.EntityId == lotDetails.EntityId).ToList();
        }

        if (existing.Count != 0)
        {
            logger.LogInformation($"Found event {lotDetails} but matched {existing.Count} already found events");
            foreach (EventDropItemLotDetails existingLotDetails in existing)
            {
                existingLotDetails.CopyFrom(logger, lotDetails);
            }
        }
        else
        {
            lotDetailsList.Add(lotDetails);
        }
    }

}