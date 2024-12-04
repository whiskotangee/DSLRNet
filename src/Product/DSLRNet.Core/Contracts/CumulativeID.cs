namespace DSLRNet.Core.Contracts;

using Serilog;
using System.Collections.Generic;

public class CumulativeID
{
    public int StartingID { get; set; } = 8000;
    public float IDMultiplier { get; set; } = 10000;
    public bool UseWrapAround { get; set; } = false;
    public int WrapAroundLimit { get; set; } = 998;
    private int cumulativeId { get; set; } = -1;

    public bool IsItemFlagAcquisitionCumulativeID { get; set; } = false;

    private Dictionary<string, object> IFA { get; set; } = new Dictionary<string, object>
    {
        { "offsets", new List<int> { 0, 4, 7, 8, 9 } },
        { "starting", 1024260000 }
    };

    private int IFA_CurrentOffset { get; set; } = 0;

    public int GetNext()
    {
        this.cumulativeId += 1;
        int IDBeforeWrap = this.cumulativeId;

        if (IDBeforeWrap > this.WrapAroundLimit)
        {
            if (this.IsItemFlagAcquisitionCumulativeID)
            {
                this.IFA_CurrentOffset += 1;
                this.IFA_CurrentOffset = this.Wrap(this.IFA_CurrentOffset, 0, ((List<int>)this.IFA["offsets"]).Count - 1);
            }
        }

        if (this.UseWrapAround)
        {
            this.cumulativeId = this.Wrap(this.cumulativeId, 0, this.WrapAroundLimit);
        }

        // Split off depending on if we're getting an ItemFlagAcquisitionID or not
        if (this.IsItemFlagAcquisitionCumulativeID)
        {
            int flagId = (int)this.IFA["starting"] + ((List<int>)this.IFA["offsets"])[this.IFA_CurrentOffset] * 1000 + this.cumulativeId;
            Log.Logger.Information($"Assigning acquisition flag {flagId}");
            return flagId;
        }
        else
        {
            return (int)((this.StartingID + this.cumulativeId) * this.IDMultiplier);
        }
    }

    private int Wrap(int value, int min, int max)
    {
        return value < min ? max : value > max ? min : value;
    }
}
