namespace DSLRNet.Core.Contracts;

using System.Collections.Generic;
using ILogger = Microsoft.Extensions.Logging.ILogger;

public class CumulativeID(ILogger logger)
{
    private readonly ILogger logger = logger;

    public int StartingID { get; set; } = 8000;
    public float IDMultiplier { get; set; } = 10000;
    public bool UseWrapAround { get; set; } = false;
    public int WrapAroundLimit { get; set; } = 998;
    private int cumulativeId { get; set; } = 0;

    public bool IsItemFlagAcquisitionCumulativeID { get; set; } = false;

    private static readonly List<int> ItemAcquisitionOffsets = [0, 4, 7, 8, 9];
    private static readonly int ItemAquisitionStartingId = 1024260000;

    private int IFA_CurrentOffset { get; set; } = 0;

    public uint GetNext()
    {
        this.cumulativeId += 1;
        int IDBeforeWrap = this.cumulativeId;

        if (IDBeforeWrap > this.WrapAroundLimit)
        {
            if (this.IsItemFlagAcquisitionCumulativeID)
            {
                this.IFA_CurrentOffset += 1;
                this.IFA_CurrentOffset = this.Wrap(this.IFA_CurrentOffset, 0, ItemAcquisitionOffsets.Count - 1);
            }
        }

        if (this.UseWrapAround)
        {
            this.cumulativeId = this.Wrap(this.cumulativeId, 0, this.WrapAroundLimit);
        }

        // Split off depending on if we're getting an ItemFlagAcquisitionID or not
        if (this.IsItemFlagAcquisitionCumulativeID)
        {
            uint flagId = (uint)(ItemAquisitionStartingId + (ItemAcquisitionOffsets)[this.IFA_CurrentOffset] * 1000 + this.cumulativeId);
            this.logger.LogInformation($"Assigning acquisition flag {flagId}");
            return flagId;
        }
        else
        {
            return (uint)((this.StartingID + this.cumulativeId) * this.IDMultiplier);
        }
    }

    private int Wrap(int value, int min, int max)
    {
        return value < min ? max : value > max ? min : value;
    }
}
