namespace DSLRNet.Core.Contracts;

public class EventDropItemLotDetails
{
    public string MapId { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public int EventTriggerFlagId { get; set; }

    public int ItemLotId { get; set; }

    public int AcquisitionFlag { get; set; }

    public int NpcId { get; set; }

    public bool IsCompleteEvent()
    {
        return EventTriggerFlagId > 0 && ItemLotId > 0 && EntityId > 0 && AcquisitionFlag > 0;
    }

    public void CopyFrom(ILogger logger, EventDropItemLotDetails from)
    {
        if (this.EventTriggerFlagId == 0 && from.EventTriggerFlagId > 0)
        {
            this.EventTriggerFlagId = from.EventTriggerFlagId;
        }
        else if (this.EventTriggerFlagId > 0 && from.EventTriggerFlagId > 0 && this.EventTriggerFlagId != from.EventTriggerFlagId)
        {
            // This is a boss event that gives items via a common event i.e. event 10000850 actually gives item via flag 9100
            logger.LogError($"EventTriggerFlagId mismatch: {this.EventTriggerFlagId} != {from.EventTriggerFlagId} when overwriting {this} with {from}");
            this.EventTriggerFlagId = from.EventTriggerFlagId;
        }

        if (this.ItemLotId == 0 && from.ItemLotId > 0)
        {
            this.ItemLotId = from.ItemLotId;
        }
        else if (this.ItemLotId > 0 && from.ItemLotId > 0 && this.ItemLotId != from.ItemLotId)
        {
            logger.LogError($"ItemLotId mismatch: {this.ItemLotId} != {from.ItemLotId} when overwriting {this} with {from}");
        }

        this.MapId ??= from.MapId;

        if (this.EntityId == 0 && from.EntityId > 0)
        {
            this.EntityId = from.EntityId;
        }
        else if (this.EntityId > 0 && from.EntityId > 0 && this.EntityId != from.EntityId)
        {
            logger.LogError($"EntityId mismatch: {this.EntityId} != {from.EntityId} when overwriting {this} with {from}");
        }

        if (this.AcquisitionFlag == 0 && from.AcquisitionFlag > 0)
        {
            this.AcquisitionFlag = from.AcquisitionFlag;
        }
        else if (this.AcquisitionFlag > 0 && from.AcquisitionFlag > 0 && this.AcquisitionFlag != from.AcquisitionFlag)
        {
            logger.LogError($"AcquisitionFlag mismatch: {this.AcquisitionFlag} != {from.AcquisitionFlag} when overwriting {this} with {from}");
        }

        if (this.NpcId == 0 && from.NpcId > 0)
        {
            this.NpcId = from.NpcId;
        }
        else if (this.NpcId > 0 && from.NpcId > 0 && this.NpcId != from.NpcId)
        {
            logger.LogError($"NpcId mismatch: {this.NpcId} != {from.NpcId} when overwriting {this} with {from}");
        }
    }
    public override string ToString()
    {
        return $"MapId: {MapId}, EntityId: {EntityId}, EventTriggerFlagIds: {EventTriggerFlagId}, ItemLotId: {ItemLotId}, AcquisitionFlag: {AcquisitionFlag}, NpcId: {NpcId}";
    }

    public static Dictionary<string, IEnumerable<EventDropItemLotDetails>> SummarizeUnsetProperties(List<EventDropItemLotDetails> detailsList)
    {
        Dictionary<string, IEnumerable<EventDropItemLotDetails>> summary = new()
        {
                { "UnsetEntityId", detailsList.Where(d => d.EntityId == 0) },
                { "UnsetEventTriggerFlagId", detailsList.Where(d => d.EventTriggerFlagId == 0) },
                { "UnsetItemLotId", detailsList.Where(d => d.ItemLotId == 0) }
            };

        return summary;
    }
}
