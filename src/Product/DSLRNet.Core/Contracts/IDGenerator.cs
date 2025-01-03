namespace DSLRNet.Core.Contracts;

using System.Collections.Generic;
using ILogger = Microsoft.Extensions.Logging.ILogger;

public class IDGenerator()
{
    private int cumulativeId = 0;
    public int StartingID { get; set; } = 8000;
    public int Multiplier { get; set; } = 10000;
    public bool IsWrapAround { get; set; } = false;
    public int WrapAroundLimit { get; set; } = 998;

    public List<int> AllowedOffsets { get; set; } = [];

    private int currentOffset = 0;

    public int GetNext()
    {
        int IDBeforeWrap = this.cumulativeId;

        if (IDBeforeWrap > this.WrapAroundLimit)
        {
            if (AllowedOffsets.Any())
            {
                this.currentOffset += 1;
                this.currentOffset = this.Wrap(this.currentOffset, 0, AllowedOffsets.Count - 1);
            }
        }

        if (this.IsWrapAround)
        {
            this.cumulativeId = this.Wrap(this.cumulativeId, 0, this.WrapAroundLimit);
        }

        try
        {
            // Split off depending on if we're getting an ItemFlagAcquisitionID or not
            if (AllowedOffsets.Any())
            {
                return this.StartingID + (this.cumulativeId + AllowedOffsets[currentOffset] * 1000);
            }
            else
            {
                return this.StartingID + (this.cumulativeId * this.Multiplier);
            }
        }
        finally
        {
            this.cumulativeId += 1;
        }
    }

    private int Wrap(int value, int min, int max)
    {
        return value < min ? max : value > max ? min : value;
    }
}
