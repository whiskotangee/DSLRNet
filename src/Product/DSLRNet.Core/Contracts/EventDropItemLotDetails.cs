namespace DSLRNet.Core.Contracts;

public class EventDropItemLotDetails
{
    public string MapId { get; set; }

    public int EntityId { get; set; }

    public int EventTriggerFlagId { get; set; }

    public int ItemLotId { get; set; }

    public int AcquisitionFlag { get; set; }

    public int NpcId { get; set; }

    public void CopyFrom(ILogger logger, EventDropItemLotDetails from)
    {
        if (this.ItemLotId == 0 && from.ItemLotId > 0)
        {
            this.ItemLotId = from.ItemLotId;
        }
        else if (this.ItemLotId > 0 && from.ItemLotId > 0 && this.ItemLotId != from.ItemLotId)
        {
            logger.LogError($"ItemLotId mismatch: {this.ItemLotId} != {from.ItemLotId} when overwriting {this.ToString()} with {from.ToString()}");
        }
        if (this.MapId == null)
        {
            this.MapId = from.MapId;
        }

        if (this.EntityId == 0 && from.EntityId > 0)
        {
            this.EntityId = from.EntityId;
        }
        else if (this.EntityId > 0 && from.EntityId > 0 && this.EntityId != from.EntityId)
        {
            logger.LogError($"EntityId mismatch: {this.EntityId} != {from.EntityId} when overwriting {this.ToString()} with {from.ToString()}");
        }

        if (this.AcquisitionFlag == 0 && from.AcquisitionFlag > 0)
        {
            this.AcquisitionFlag = from.AcquisitionFlag;
        }
        else if (this.AcquisitionFlag > 0 && from.AcquisitionFlag > 0 && this.AcquisitionFlag != from.AcquisitionFlag)
        {
            logger.LogError($"AcquisitionFlag mismatch: {this.AcquisitionFlag} != {from.AcquisitionFlag} when overwriting {this.ToString()} with {from.ToString()}");
        }

        if (this.NpcId == 0 && from.NpcId > 0)
        {
            this.NpcId = from.NpcId;
        }
        else if (this.NpcId > 0 && from.NpcId > 0 && this.NpcId != from.NpcId)
        {
            logger.LogError($"NpcId mismatch: {this.NpcId} != {from.NpcId} when overwriting {this.ToString()} with {from.ToString()}");
        }
    }
    public override string ToString()
    {
        return $"MapId: {MapId}, EntityId: {EntityId}, EventTriggerFlagIds: {EventTriggerFlagId}, ItemLotId: {ItemLotId}, AcquisitionFlag: {AcquisitionFlag}, NpcId: {NpcId}";
    }

    public static Dictionary<string, IEnumerable<EventDropItemLotDetails>> SummarizeUnsetProperties(List<EventDropItemLotDetails> detailsList)
    {
        var summary = new Dictionary<string, IEnumerable<EventDropItemLotDetails>>
            {
                { "UnsetEntityId", detailsList.Where(d => d.EntityId == 0) },
                { "UnsetEventTriggerFlagId", detailsList.Where(d => d.EventTriggerFlagId == 0) },
                { "UnsetItemLotId", detailsList.Where(d => d.ItemLotId == 0) }
            };

        return summary;
    }
}
