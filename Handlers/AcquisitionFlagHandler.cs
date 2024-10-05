using DSLRNet.Contracts;
using DSLRNet;
using DSLRNet.Handlers;
using DSLRNet.Config;
using Microsoft.Extensions.Options;
using DSLRNet.Data;

namespace DSLRNet;

public class AcquisitionFlagHandler(
    IOptions<Configuration> configuration,
    DataRepository dataRepository,
    CumulativeID cumulativeId) : BaseHandler(dataRepository)
{
    private readonly Configuration configuration = configuration.Value;

    public CumulativeID CumulativeID { get; set; } = cumulativeId;

    public int AdditionPerCycle { get; set; } = 10000;

    private int AcquisitionIDStart { get; set; } = 1024260000;

    // Variable to choose which flag x000 offset we're using
    private int FlagRangeOffset { get; set; } = 0;
    // If we fill every flag in the range 0000-9999, increase this
    private int OverallAcquisitionIDMultiplier { get; set; } = 0;

    private List<int> SaveableFlags { get; set; } = [];

    public void ResetAcquisitionFlagHandler()
    {
        CumulativeID.ResetCumulativeID();
        OverallAcquisitionIDMultiplier = 0;
        FlagRangeOffset = 0;
    }

    public void SetupAcquisitionFlagHandler()
    {
        AcquisitionIDStart = GetAcquisitionIdStartFromGameType();
        SaveableFlags = GetAcquisitionSavedFlagsArrayFromGameType();
        CumulativeID.WrappingAround += OnCumulativeIdWrapAround;
    }

    private void OnCumulativeIdWrapAround()
    {
        FlagRangeOffset += 1;
        OverallAcquisitionIDMultiplier += 1;
        FlagRangeOffset = Wrap(FlagRangeOffset, 0, SaveableFlags.Count - 1);
    }

    public int GetCurrentAcquisitionFlag()
    {
        int newAcquisitionFlag = AcquisitionIDStart + (SaveableFlags[FlagRangeOffset] * 1000) + CumulativeID.GetNext();
        // Console.WriteLine(newAcquisitionFlag);
        return newAcquisitionFlag;
    }

    private int GetAcquisitionIdStartFromGameType()
    {
        return this.configuration.Flags.ItemAcquisitionOffset;
    }

    private List<int> GetAcquisitionSavedFlagsArrayFromGameType()
    {
        return this.configuration.Flags.SaveableAcquisitionFlags;
    }

    private int Wrap(int value, int min, int max)
    {
        return (value < min) ? max : (value > max) ? min : value;
    }
}
