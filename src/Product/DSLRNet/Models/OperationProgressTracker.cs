namespace DSLRNet.Models
{
    public class OperationProgressTracker : BaseModel<OperationProgressTracker>, IOperationProgressTracker
    {
        private int overallProgress;
        private int overallStepCount;
        private int currentStageProgress;
        private int currentStageStepCount;
        private int generatedMapItemLots;
        private int generatedEnemyItemLots;
        private int generatedBossItemLots;
        private int generatedWeapons;
        private int generatedArmor;
        private int generatedTalismans;

        public OperationProgressTracker()
        {
            Reset();
        }

        public int OverallProgress
        {
            get => overallProgress;
            set
            {
                if (overallProgress != value)
                {
                    overallProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentStageProgress
        {
            get => currentStageProgress;
            set
            {
                if (currentStageProgress != value)
                {
                    currentStageProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public int GeneratedMapItemLots
        {
            get => generatedMapItemLots;
            set
            {
                if (generatedMapItemLots != value)
                {
                    generatedMapItemLots = value;
                    OnPropertyChanged();
                }
            }
        }

        public int GeneratedEnemyItemLots
        {
            get => generatedEnemyItemLots;
            set
            {
                if (generatedEnemyItemLots != value)
                {
                    generatedEnemyItemLots = value;
                    OnPropertyChanged();
                }
            }
        }

        public int GeneratedWeapons
        {
            get => generatedWeapons;
            set
            {
                if (generatedWeapons != value)
                {
                    generatedWeapons = value;
                    OnPropertyChanged();
                }
            }
        }

        public int GeneratedArmor
        {
            get => generatedArmor;
            set
            {
                if (generatedArmor != value)
                {
                    generatedArmor = value;
                    OnPropertyChanged();
                }
            }
        }

        public int GeneratedTalismans
        {
            get => generatedTalismans;
            set
            {
                if (generatedTalismans != value)
                {
                    generatedTalismans = value;
                    OnPropertyChanged();
                }
            }
        }

        public int OverallStepCount
        {
            get => overallStepCount;
            set
            {
                if (overallStepCount != value)
                {
                    overallStepCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentStageStepCount
        {
            get => currentStageStepCount;
            set
            {
                if (currentStageStepCount != value)
                {
                    currentStageStepCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public void Reset()
        {
            OverallProgress = 0;
            OverallStepCount = 1;
            CurrentStageProgress = 0;
            CurrentStageStepCount = 1;
            GeneratedMapItemLots = 0;
            GeneratedEnemyItemLots = 0;
            GeneratedWeapons = 0;
            GeneratedArmor = 0;
            GeneratedTalismans = 0;
        }
    }
}
