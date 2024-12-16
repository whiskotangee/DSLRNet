namespace DSLRNet.Core.Scan;

using System.Net.Http.Headers;
using static SoulsFormats.EMEVD.Instruction;

public class BossDropScanner(ILogger<BossDropScanner> logger, IOptions<Configuration> config)
{
    private readonly Configuration configuration = config.Value;

    public List<EventDropItemLotDetails> ScanEventsForBossDrops()
    {
        List<EventDropItemLotDetails> lotDetails = [];

        //load common emved
        var commonEmevdFile = Path.Combine(configuration.Settings.DeployPath, "event", "common.emevd.dcx");
        if (!File.Exists(commonEmevdFile))
        {
            commonEmevdFile = Path.Combine(configuration.Settings.GamePath, "event", "common.emevd.dcx");
        }

        EMEVD emevd = EMEVD.Read(commonEmevdFile);

        foreach (var ev in emevd.Events)
        {
            foreach (var instruction in ev.Instructions)
            {
                // InitializeCommonEvent
                if (instruction.Bank == 2000)
                {
                    var instructionCount = instruction.ArgData.Length / 4;

                    List<object> args = instruction.UnpackArgs(Enumerable.Repeat(ArgType.Int32, instructionCount));

                    if (args.Count == 6)
                    {
                        var eventId = Convert.ToInt32(args[1]);

                        // we want 1100 (vanilla) and 1200 (dlc) for boss kills
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

        var otherEmveds = Directory.GetFiles(Path.Combine(configuration.Settings.DeployPath, "event"), "m*emevd.dcx").ToList();
        var additionalEmveds = Directory.GetFiles(Path.Combine(configuration.Settings.GamePath, "event"), "m*emevd.dcx")
            .Where(d => !otherEmveds.Any(s => Path.GetFileName(s) == Path.GetFileName(d)))
            .ToList();

        otherEmveds.AddRange(additionalEmveds);

        foreach (var mapEventFile in otherEmveds.Distinct())
        {
            EMEVD mapEmevd = EMEVD.Read(mapEventFile);

            var mapId = Path.GetFileName(mapEventFile);
            mapId = mapId.Substring(0, mapId.IndexOf('.'));

            logger.LogInformation($"Scanning events from {mapId} for boss drops");

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
                    // Unknown200476 - grants items in some cases for events
                    // bosses like the dancer in the dlc use this
                    else if (instruction.Bank == 2004 && instruction.ID == 76)
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

                            existingMapping.EntityId = flagId; // it appears the flag is the entityId
                            existingMapping.EventTriggerFlagId = flagId;
                            existingMapping.ItemLotId = itemLotId;
                        }
                    }
                    // HandleBossDefeatAndDisplayBanner - boss is killed
                    else if (instruction.Bank == 2003 && instruction.ID == 12)
                    {
                        entityId = Convert.ToInt32(args[0]);
                        lookingForEventId = true;
                    }
                    // SetEventFlagID - Set flag to award item from emevd commons
                    else if (instruction.Bank == 2003 && instruction.ID == 66 && lookingForEventId)
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
                    //AwardItemsIncludingClients - open world bosses use this to award items (maybe only in dlc?)
                    else if (instruction.Bank == 2003 && instruction.ID == 33 && lookingForEventId)
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
            }
        }

        logger.LogInformation($"EventFlags that don't have mapid - {string.Join(",", lotDetails.Where(d => string.IsNullOrEmpty(d.MapId)).Select(d => d.EventTriggerFlagId))}");

        return lotDetails;
    }
}
