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
        cumulativeId += 1;
        int IDBeforeWrap = cumulativeId;

        if (IDBeforeWrap > WrapAroundLimit)
        {
            if (IsItemFlagAcquisitionCumulativeID)
            {
                IFA_CurrentOffset += 1;
                IFA_CurrentOffset = Wrap(IFA_CurrentOffset, 0, ((List<int>)IFA["offsets"]).Count - 1);
            }
        }

        if (UseWrapAround)
        {
            cumulativeId = Wrap(cumulativeId, 0, WrapAroundLimit);
        }

        // Split off depending on if we're getting an ItemFlagAcquisitionID or not
        if (IsItemFlagAcquisitionCumulativeID)
        {
            var flagId = (int)IFA["starting"] + ((List<int>)IFA["offsets"])[IFA_CurrentOffset] * 1000 + cumulativeId;
            Log.Logger.Information($"Assigning acquisition flag {flagId}");
            return flagId;
        }
        else
        {
            return (int)((StartingID + cumulativeId) * IDMultiplier);
        }
    }

    private int Wrap(int value, int min, int max)
    {
        return value < min ? max : value > max ? min : value;
    }
}
