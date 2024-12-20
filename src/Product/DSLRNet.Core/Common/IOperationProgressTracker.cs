public interface IOperationProgressTracker
{
    // Overall progress of the operation (0 to 100)
    int OverallProgress { get; set; }

    int OverallStepCount { get; set; }

    // Current stage progress of the operation (0 to 100)
    int CurrentStageProgress { get; set; }

    int CurrentStageStepCount { get; set; }

    // Count of generated map item lots
    int GeneratedMapItemLots { get; set; }

    // Count of generated enemy item lots
    int GeneratedEnemyItemLots { get; set; }

    // Count of generated weapons
    int GeneratedWeapons { get; set; }

    // Count of generated armor
    int GeneratedArmor { get; set; }

    // Count of generated talismans
    int GeneratedTalismans { get; set; }

    // Method to reset all progress and counts
    void Reset();
}
