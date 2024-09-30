namespace DSLRNet.Contracts;

using System;
using System.Collections.Generic;

public class CumulativeID
{
    public int StartingID { get; set; } = 8000;
    public double IDMultiplier { get; set; } = 10000;
    public int AmountPerIncrement { get; set; } = 1;
    public bool UseWrapAround { get; set; } = false;
    public int WrapAroundLimit { get; set; } = 998;
    private int cumulativeId { get; set; } = -1;

    private const int CumulativeIDStartingPoint = -1;

    // ITEMFLAGACQUISITION VARIABLES
    public bool IsItemFlagAcquisitionCumulativeID { get; set; } = false;
    private Dictionary<string, object> IFA { get; set; } = new Dictionary<string, object>
    {
        { "offsets", new List<int> { 0 } },
        { "starting", 1024260000 }
    };
    private int IFA_CurrentOffset { get; set; } = 0;

    public event Action WrappingAround;

    public int GetNext()
    {
        cumulativeId += 1;
        int IDBeforeWrap = cumulativeId;

        // Emit signal if we've gone over the wrap around limit
        if (IDBeforeWrap > WrapAroundLimit)
        {
            WrappingAround?.Invoke();
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
            return (int)IFA["starting"] + ((List<int>)IFA["offsets"])[IFA_CurrentOffset] * 1000 + cumulativeId;
        }
        else
        {
            return (int)((StartingID + cumulativeId) * IDMultiplier);
        }
    }

    public void ResetCumulativeID()
    {
        cumulativeId = CumulativeIDStartingPoint;
        Console.WriteLine($"{this.GetType().Name} CUMULATIVE ID RESETTING!");
        IFA_CurrentOffset = 0;
    }

    // ITEMFLAGACQUISITION FUNCTIONS

    private int Wrap(int value, int min, int max)
    {
        return (value < min) ? max : (value > max) ? min : value;
    }
}
