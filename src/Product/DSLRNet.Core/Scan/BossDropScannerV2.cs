namespace DSLRNet.Core.Scan;

using DSLRNet.Core.DAL;
using System.Diagnostics.CodeAnalysis;

using static SoulsFormats.EMEVD.Instruction;

public class BossDropScannerV2(ILogger<BossDropScanner> logger, IOptions<Configuration> config, FileSourceHandler fileHandler, DataAccess dataAccess)
{
    public List<EventDropItemLotDetails> ScanEventsForBossDrops()
    {
        // start with GameAreaParam, these are boss entity ids

        var bossDeathFlags = new Dictionary<uint, int>();
        foreach (var gameAreaParam in dataAccess.GameAreaParam.GetAll())
        {
            if (gameAreaParam.defeatBossFlagId > 0)
            {
                bossDeathFlags.TryAdd(gameAreaParam.defeatBossFlagId, gameAreaParam.ID);
            }
        }

        logger.LogWarning($"Bosses missing event drops {bossDeathFlags.Count()} - {string.Join(",", bossDeathFlags.Select(d => d.Key))}");

        // scan common emevds for flags that give item lots
        logger.LogInformation($"Scanning common events for boss drops");

        List<EventDropItemLotDetails> commonEventsDetails = [];
        List<EventDropItemLotDetails> lotDetails = [];
        var commonEmevdFile = GetCommonEmevdFile("common.emevd.dcx");

        EMEVD emevd = EMEVD.Read(commonEmevdFile);
        ScanMapEvents(emevd, Path.GetFileNameWithoutExtension(commonEmevdFile), commonEventsDetails, lotDetails);

        var filteredBossFlags = bossDeathFlags.Where(d => !lotDetails.Any(s => s.EventTriggerFlagId == d.Key && s.ItemLotId > 0)).ToDictionary(d => d.Key, d => d.Value);

        if (filteredBossFlags.Any())
        {
            logger.LogWarning($"Bosses missing event drops {filteredBossFlags.Count()} - {string.Join(",", filteredBossFlags.Select(d => d.Key))}");
        }

        // scan all other emevds, look for boss defeat events, then for setting flags that trigger item lots

        //var otherEmveds = fileHandler.ListFilesFromAllModDirectories("event", "m60_42_38*emevd.dcx");
        var otherEmveds = fileHandler.ListFilesFromAllModDirectories("event", "m*emevd.dcx");
        foreach (var mapEventFile in otherEmveds.Distinct())
        {
            EMEVD mapEmevd = EMEVD.Read(mapEventFile);
            var mapId = GetMapId(mapEventFile);
            logger.LogInformation($"Scanning events from {mapId} for boss drops");
            ScanMapEvents(mapEmevd, mapId, commonEventsDetails, lotDetails);

            filteredBossFlags = filteredBossFlags.Where(d => !lotDetails.Any(s => s.EventTriggerFlagId == d.Key && s.ItemLotId > 0)).ToDictionary(d => d.Key, d => d.Value);

            logger.LogWarning($"Bosses missing event drops {filteredBossFlags.Count()} - {string.Join(",", filteredBossFlags.Select(d => d.Key))}");
        }

        var missingCommonEvents = commonEventsDetails.Where(d => d.ItemLotId > 0 && !lotDetails.Any(s => s.EventTriggerFlagId == d.EventTriggerFlagId)).ToList();
        lotDetails.AddRange(missingCommonEvents);
        //lotDetails.AddRange(commonEventsDetails.Where(d => !lotDetails.Any(s => s.ItemLotId == d.ItemLotId)));

        filteredBossFlags = filteredBossFlags.Where(d => !lotDetails.Any(s => (s.EventTriggerFlagId == d.Key || s.EntityId == d.Key) && s.ItemLotId > 0)).ToDictionary(d => d.Key, d => d.Value);

        logger.LogWarning($"Bosses missing event drops {filteredBossFlags.Count()} - {string.Join(",", filteredBossFlags.Select(d => d.Key))}");

        logger.LogInformation($"Found {lotDetails.Where(d => d.ItemLotId > 0).DistinctBy(d => d.ItemLotId).Count()} boss drop events");

        var res = EventDropItemLotDetails.SummarizeUnsetProperties(lotDetails);
        foreach (var toLog in res)
        {
            logger.LogInformation($"{toLog.Key} - {string.Join(Environment.NewLine, toLog.Value)}");  
        }

        throw new Exception($"Stop early");
        //return [.. lotDetails];
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
        var mapId = Path.GetFileName(mapEventFile);
        return mapId[..mapId.IndexOf('.')];
    }

    private bool TryHandleCommonEventInstruction(string mapName, EMEVD emevd, EMEVD.Event ev, EMEVD.Instruction instruction, List<long> args, [NotNullWhen(true)] out EventDropItemLotDetails? lotDetails)
    {
        lotDetails = null;

        var bossEventConfig = config.Value.ScannerConfig.CommonBossEventConfigs.SingleOrDefault(d => d.BankId == instruction.Bank && d.InstructionId == instruction.ID && d.EventId == Convert.ToInt64(args[d.EventIdIndex]));
        if (bossEventConfig != null)
        {
            var details = new EventDropItemLotDetails();

            if (bossEventConfig.EventFlagIndex > 0)
            {
                details.EventTriggerFlagId = Convert.ToInt32(args[bossEventConfig.EventFlagIndex]);
            }
            if (bossEventConfig.ItemLotIdIndex > 0)
            {
                var itemLotId = Convert.ToInt32(args[bossEventConfig.ItemLotIdIndex]);
                if (dataAccess.ItemLotParamMap.GetItemById(itemLotId) == null)
                {
                    logger.LogError($"{mapName} contains event id {ev.ID} and instruction {instruction.Bank}[{instruction.ID}] which references itemLotId {itemLotId} that does not exist in the item lot param");
                    return false;
                }
                else
                {
                    details.ItemLotId = itemLotId;
                }
            }
            if (bossEventConfig.AcquisitionFlagIndex > 0)
            {
                details.AcquisitionFlag = Convert.ToInt32(args[bossEventConfig.AcquisitionFlagIndex]);
            }
            if (bossEventConfig.EntityIdIndex > 0)
            {
                details.EntityId = Convert.ToInt32(args[bossEventConfig.EntityIdIndex]);
            }

            logger.LogDebug($"{ev.ID} and instruction {instruction.Bank}[{instruction.ID}] has boss drop config {details.ToString()}");

            lotDetails = details;
            return true;
        }
        else if (instruction.Bank == 2000 && instruction.ID == 0 && ev.ID == 0 && args.Count == 8)
        {
            var eventFlag = (int)args[1];
            var entityId = (int)args[2];
            var itemLot = (int)args[6];

            if (itemLot == 0 || dataAccess.ItemLotParamMap.GetItemById(itemLot) == null)
            {
                return false;
            }

            lotDetails = new EventDropItemLotDetails()
            {
                EntityId = entityId,
                EventTriggerFlagId = eventFlag,
                ItemLotId = itemLot,
                MapId = mapName
            };

            logger.LogDebug($"{ev.ID} and instruction {instruction.Bank}[{instruction.ID}] did not match config, but still valid {lotDetails}");
            return true;
        }

        return false;
    }

    private void ScanMapEvents(EMEVD mapEmevd, string mapId, List<EventDropItemLotDetails> commonEventsDetails, List<EventDropItemLotDetails> lotDetailsList)
    {
        foreach (var ev in mapEmevd.Events)
        {
            int entityId = 0;
            bool isBossDefeatedEvent = false;

            var lotDetails = new EventDropItemLotDetails();

            foreach (var instruction in ev.Instructions)
            {
                var instructionCount = instruction.ArgData.Length / 4;
                List<long> args = instruction.UnpackArgs(Enumerable.Repeat(ArgType.Int32, instructionCount)).Select(Convert.ToInt64).ToList();

                logger.LogTrace($"Scanning {mapId} event {ev.ID} instruction {instruction.Bank}[{instruction.ID}] with args {string.Join(',', args)}");

                if (TryHandleCommonEventInstruction(mapId, mapEmevd, ev, instruction, args, out EventDropItemLotDetails? commonEventDetails))
                {
                    isBossDefeatedEvent = true;
                    if (lotDetails.EventTriggerFlagId == commonEventDetails.EventTriggerFlagId)
                    {
                        lotDetails.CopyFrom(logger, commonEventDetails);
                    }
                    else
                    {
                        logger.LogDebug($"TryHandleCommonEventInstruction {lotDetails.ToString()} being set to {commonEventDetails.ToString()}");
                        if (lotDetails.EventTriggerFlagId > 0)
                        {
                            lotDetailsList.Add(lotDetails);
                        }
                        lotDetails = commonEventDetails;
                    }

                    commonEventsDetails.Add(commonEventDetails);
                    logger.LogDebug($"TryHandleCommonEventInstruction {lotDetails.ToString()}");
                    continue;
                }
                // AwardItemLot
                else if (instruction.Bank == 2003 && instruction.ID == 4)
                {
                    ProcessAwardItemLot(args, mapId, lotDetails);
                    logger.LogDebug($"AwardItemLot {lotDetails.ToString()}");
                }
                // Unknown200476 - used for some open world boss drops
                // bosses like the dancer in the dlc use this
                else if (instruction.Bank == 2004 && instruction.ID == 76)
                {
                    ProcessUnknown200476(args, mapId, lotDetails);
                    logger.LogDebug($"Unknown200476 {lotDetails.ToString()}");
                }
                // HandleBossDefeatAndDisplayBanner - boss is killed
                else if (instruction.Bank == 2003 && instruction.ID == 12)
                {
                    entityId = Convert.ToInt32(args[0]);
                    lotDetails.EntityId = entityId;
                    isBossDefeatedEvent = true;
                    logger.LogDebug($"HandleBossDefeatAndDisplayBanner {lotDetails.ToString()}");
                }
                // SetEventFlagID - sets flag events to trigger common event drops
                else if (instruction.Bank == 2003 && instruction.ID == 66)
                {
                    var foundFlag = ProcessSetEventFlagID(args, entityId, mapId, lotDetails);

                    var commonEvent = commonEventsDetails.SingleOrDefault(d => d.EventTriggerFlagId == lotDetails.EventTriggerFlagId);
                    if (commonEvent != null)
                    {
                        lotDetails.CopyFrom(logger, commonEvent);
                    }
                    
                    commonEvent = commonEventsDetails.SingleOrDefault(d => d.EventTriggerFlagId == foundFlag);
                    if (commonEvent != null)
                    {
                        lotDetails.CopyFrom(logger, commonEvent);
                    }

                    logger.LogDebug($"SetEventFlagID {lotDetails.ToString()}");
                }
                // AwardItemsIncludingClients - used for some open world boss drops
                // bosses like the blackgaol knight, fort of reprimand knight, etc in the dlc use this
                else if (instruction.Bank == 2003 && instruction.ID == 36)
                {
                    ProcessAwardItemsIncludingClients(args, entityId, mapId, lotDetails);
                    logger.LogDebug($"AwardItemsIncludingClients {lotDetails.ToString()}");
                }
            }

            if (isBossDefeatedEvent)
            {
                if (lotDetails.EventTriggerFlagId == 0 || lotDetails.ItemLotId == 0)
                {
                    logger.LogError($"Boss defeated event {lotDetails.ToString()} missing event trigger flag id");
                    continue;
                }

                var existing = lotDetailsList.Where(d =>
                    d.EventTriggerFlagId == lotDetails.EventTriggerFlagId && d.EntityId == lotDetails.EntityId).ToList();

                if (existing.Any())
                {
                    logger.LogInformation($"Found boss defeated event {lotDetails.ToString()} but matched {existing.Count} other events via flag Id and entity id");
                    // fill in existing with lotDetails if it's set to default
                    foreach (var existingLotDetails in existing)
                    {
                        existingLotDetails.CopyFrom(logger, lotDetails);
                    }
                }
                else
                {
                    var existingByFlagId = lotDetailsList.SingleOrDefault(d => d.EventTriggerFlagId == lotDetails.EventTriggerFlagId);
                    if (existingByFlagId != null)
                    {
                        existingByFlagId.CopyFrom(logger, lotDetails);
                    }
                    else
                    {
                        lotDetailsList.Add(lotDetails);
                    }
                }

                logger.LogInformation($"Found boss defeated event {lotDetails.ToString()} - was {(existing.Any() ? "updated" : "added")}");
            }
        }
    }

    private void ProcessAwardItemLot(List<long> args, string mapId, EventDropItemLotDetails lotDetails)
    {
        var itemLotId = Convert.ToInt32(args[0]);
        if (itemLotId > 0)
        {
            lotDetails.ItemLotId = itemLotId;
            lotDetails.MapId = mapId;
        }
    }

    private void ProcessUnknown200476(List<long> args, string mapId, EventDropItemLotDetails lotDetails)
    {
        var flagId = Convert.ToInt32(args[0]);
        var itemLotId = Convert.ToInt32(args[1]);

        if (itemLotId > 0)
        {
            lotDetails.EntityId = flagId;
            lotDetails.EventTriggerFlagId = flagId;
            lotDetails.ItemLotId = itemLotId;
            lotDetails.MapId = mapId;
        }
    }

    private int ProcessSetEventFlagID(List<long> args, int entityId, string mapId, EventDropItemLotDetails lotDetails)
    {
        var flagId = Convert.ToInt32(args[1]);
        if (flagId > 0)
        {
            if (flagId == lotDetails.EventTriggerFlagId || lotDetails.EventTriggerFlagId == 0)
            {
                lotDetails.EntityId = entityId;
                lotDetails.EventTriggerFlagId = flagId;
                lotDetails.MapId = mapId;
            }
            else if (lotDetails.EventTriggerFlagId > 0)
            {
                logger.LogError($"FlagId mismatch: {lotDetails.EventTriggerFlagId} != {flagId} when overwriting {lotDetails.ToString()}");
            }
        }

        return flagId;
    }

    private void ProcessAwardItemsIncludingClients(List<long> args, int entityId, string mapId, EventDropItemLotDetails lotDetails)
    {
        var itemLotId = Convert.ToInt32(args[0]);
        if (itemLotId > 0)
        {
            lotDetails.ItemLotId = itemLotId;
            lotDetails.EntityId = entityId;
            lotDetails.MapId = mapId;
        }
    }
}