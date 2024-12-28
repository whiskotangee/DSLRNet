namespace DSLRNet.Core.Common;

internal class DefaultProgressTracker : IOperationProgressTracker
{
    public int OverallProgress { get ; set ; }
    public int OverallStepCount { get ; set ; }
    public int CurrentStageProgress { get ; set ; }
    public int CurrentStageStepCount { get ; set ; }
    public int GeneratedMapItemLots { get ; set ; }
    public int GeneratedEnemyItemLots { get ; set ; }
    public int GeneratedWeapons { get ; set ; }
    public int GeneratedArmor { get ; set ; }
    public int GeneratedTalismans { get ; set ; }

    public void Reset()
    {
        this.OverallProgress = 0;
        this.OverallStepCount = 0;
        this.CurrentStageProgress = 0;
        this.CurrentStageStepCount = 0;
        this.GeneratedMapItemLots = 0;
        this.GeneratedEnemyItemLots = 0;
        this.GeneratedWeapons = 0;
        this.GeneratedArmor = 0;
        this.GeneratedTalismans = 0;
    }
}
