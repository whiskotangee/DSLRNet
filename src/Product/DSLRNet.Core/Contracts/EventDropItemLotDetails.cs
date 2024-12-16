namespace DSLRNet.Core.Contracts;

public class EventDropItemLotDetails
{
    public string MapId { get; set; }

    public int EntityId { get; set; }

    public int EventTriggerFlagId { get; set; }

    public int ItemLotId { get; set; }

    public int AcquisitionFlag { get; set; }

    public int NpcId { get; set; }
}
