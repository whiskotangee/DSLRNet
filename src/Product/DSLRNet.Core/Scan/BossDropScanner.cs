namespace DSLRNet.Core.Scan;

using static SoulsFormats.EMEVD.Instruction;

public class BossDropScanner(ILogger<BossDropScanner> logger, IOptions<Configuration> config, IOptions<Settings> settings)
{
    private readonly Configuration configuration = config.Value;
    private readonly Settings settings = settings.Value;

    public List<EventDropItemLotDetails> ScanEventsForBossDrops()
    {
        List<EventDropItemLotDetails> lotDetails = new();

        var commonEmevdFile = GetCommonEmevdFile();
        if (commonEmevdFile != null)
        {
            EMEVD emevd = EMEVD.Read(commonEmevdFile);
            ScanCommonEvents(emevd, lotDetails);
        }

        var otherEmveds = GetOtherEmevdFiles();
        foreach (var mapEventFile in otherEmveds.Distinct())
        {
            EMEVD mapEmevd = EMEVD.Read(mapEventFile);
            var mapId = GetMapId(mapEventFile);
            logger.LogInformation($"Scanning events from {mapId} for boss drops");
            ScanMapEvents(mapEmevd, mapId, lotDetails);
        }

        logger.LogInformation($"EventFlags that don't have mapid - {string.Join(",", lotDetails.Where(d => string.IsNullOrEmpty(d.MapId)).Select(d => d.EventTriggerFlagId))}");

        return lotDetails;
    }

    private string GetCommonEmevdFile()
    {
        var commonEmevdFile = Path.Combine(settings.DeployPath, "event", "common.emevd.dcx");
        if (!File.Exists(commonEmevdFile))
        {
            commonEmevdFile = Path.Combine(settings.GamePath, "event", "common.emevd.dcx");
        }
        return commonEmevdFile;
    }

    private List<string> GetOtherEmevdFiles()
    {
        var otherEmveds = Directory.GetFiles(Path.Combine(settings.DeployPath, "event"), "m*emevd.dcx").ToList();
        var additionalEmveds = Directory.GetFiles(Path.Combine(settings.GamePath, "event"), "m*emevd.dcx")
            .Where(d => !otherEmveds.Any(s => Path.GetFileName(s) == Path.GetFileName(d)))
            .ToList();
        otherEmveds.AddRange(additionalEmveds);
        return otherEmveds;
    }

    private string GetMapId(string mapEventFile)
    {
        var mapId = Path.GetFileName(mapEventFile);
        return mapId.Substring(0, mapId.IndexOf('.'));
    }

    private void ScanCommonEvents(EMEVD emevd, List<EventDropItemLotDetails> lotDetails)
    {
        foreach (var ev in emevd.Events)
        {
            foreach (var instruction in ev.Instructions)
            {
                if (instruction.Bank == 2000)
                {
                    var instructionCount = instruction.ArgData.Length / 4;
                    List<object> args = instruction.UnpackArgs(Enumerable.Repeat(ArgType.Int32, instructionCount));

                    if (args.Count == 6)
                    {
                        var eventId = Convert.ToInt32(args[1]);
                        if (eventId == 1100 || eventId == 1200)
                        {
                            var flagId = Convert.ToInt32(args[2]);
                            var itemLotId = Convert.ToInt32(args[3]);
                            var acquisitionFlag = Convert.ToInt32(args[5]);
                            logger.LogInformation($"Found common event {flagId} awarding itemLot {itemLotId} with acquisitionFlag {acquisitionFlag}");
                            lotDetails.Add(new EventDropItemLotDetails()
                            {
                                EventTriggerFlagId = flagId,
                                ItemLotId = itemLotId,
                                AcquisitionFlag = acquisitionFlag,
                            });
                        }
                    }
                }
            }
        }
    }

    private void ScanMapEvents(EMEVD mapEmevd, string mapId, List<EventDropItemLotDetails> lotDetails)
    {
        foreach (var ev in mapEmevd.Events)
        {
            bool lookingForEventId = false;
            int entityId = 0;

            foreach (var instruction in ev.Instructions)
            {
                var instructionCount = instruction.ArgData.Length / 4;
                List<object> args = instruction.UnpackArgs(Enumerable.Repeat(ArgType.Int32, instructionCount));

                // AwardItemLot
                if (instruction.Bank == 2003 && instruction.ID == 4)
                {
                    ProcessAwardItemLot(args, mapId, lotDetails);
                }
                // Unknown200476 - used for some open world boss drops
                // bosses like the dancer in the dlc use this
                else if (instruction.Bank == 2004 && instruction.ID == 76)
                {
                    ProcessUnknown200476(args, mapId, lotDetails);
                }
                // HandleBossDefeatAndDisplayBanner - boss is killed
                else if (instruction.Bank == 2003 && instruction.ID == 12)
                {
                    entityId = Convert.ToInt32(args[0]);
                    lookingForEventId = true;
                }
                // SetEventFlagID - sets flag events to trigger common event drops
                else if (instruction.Bank == 2003 && instruction.ID == 66 && lookingForEventId)
                {
                    ProcessSetEventFlagID(args, entityId, mapId, lotDetails, ref lookingForEventId);
                }
                // AwardItemsIncludingClients - used for some open world boss drops
                // bosses like the blackgaol knight in the dlc use this
                else if (instruction.Bank == 2003 && instruction.ID == 33 && lookingForEventId)
                {
                    ProcessAwardItemsIncludingClients(args, entityId, mapId, lotDetails);
                }
            }
        }
    }

    private void ProcessAwardItemLot(List<object> args, string mapId, List<EventDropItemLotDetails> lotDetails)
    {
        var itemLotId = Convert.ToInt32(args[0]);
        if (itemLotId > 0)
        {
            var existingMapping = lotDetails.SingleOrDefault(d => d.ItemLotId == itemLotId);
            if (existingMapping == null)
            {
                existingMapping = new EventDropItemLotDetails
                {
                    ItemLotId = itemLotId
                };
                lotDetails.Add(existingMapping);
            }
            existingMapping.MapId = mapId;
        }
    }

    private void ProcessUnknown200476(List<object> args, string mapId, List<EventDropItemLotDetails> lotDetails)
    {
        var flagId = Convert.ToInt32(args[0]);
        var itemLotId = Convert.ToInt32(args[1]);

        if (itemLotId > 0)
        {
            var existingMapping = lotDetails.SingleOrDefault(d => d.EventTriggerFlagId == flagId || d.ItemLotId == itemLotId);
            if (existingMapping == null)
            {
                existingMapping = new EventDropItemLotDetails
                {
                    MapId = mapId,
                };
                lotDetails.Add(existingMapping);
            }

            existingMapping.EntityId = flagId;
            existingMapping.EventTriggerFlagId = flagId;
            existingMapping.ItemLotId = itemLotId;
        }
    }

    private void ProcessSetEventFlagID(List<object> args, int entityId, string mapId, List<EventDropItemLotDetails> lotDetails, ref bool lookingForEventId)
    {
        var flagId = Convert.ToInt32(args[1]);
        if (flagId > 0)
        {
            var existingMapping = lotDetails.SingleOrDefault(d => d.EventTriggerFlagId == flagId);

            if (existingMapping != null)
            {
                logger.LogInformation($"Adding entityId {entityId} and mapId {mapId} to mapping for flag {flagId} and itemLot {existingMapping.ItemLotId}");
                lookingForEventId = false;
                existingMapping.EntityId = entityId;
                existingMapping.MapId = mapId;
            }
        }
    }

    private void ProcessAwardItemsIncludingClients(List<object> args, int entityId, string mapId, List<EventDropItemLotDetails> lotDetails)
    {
        var itemLotId = Convert.ToInt32(args[0]);
        if (itemLotId > 0)
        {
            var existingMapping = lotDetails.SingleOrDefault(d => d.ItemLotId == itemLotId);
            if (existingMapping == null)
            {
                existingMapping = new EventDropItemLotDetails
                {
                    ItemLotId = itemLotId
                };
                lotDetails.Add(existingMapping);
            }

            existingMapping.EntityId = entityId;
            existingMapping.MapId = mapId;
        }
    }
}