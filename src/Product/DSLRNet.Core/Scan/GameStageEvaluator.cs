namespace DSLRNet.Core.Scan;

using DSLRNet.Core.Extensions;
using System.Collections.Concurrent;

public class GameStageEvaluator
{
    private readonly ILogger<GameStageEvaluator> logger;
    private readonly List<NpcParam> npcParams;
    private readonly Configuration configuration;
    private readonly List<SpEffectParam> areaScalingSpEffects;
    private readonly List<float> vanillaScaleScores;
    private readonly List<float> dlcScaleScores;

    private ConcurrentDictionary<int, (Dictionary<float, int> vanilla, Dictionary<float, int> dlc)> hpMultCache = [];

    public GameStageEvaluator(ILogger<GameStageEvaluator> logger, IOptions<Configuration> config, IDataSource<SpEffectParam> spEffectParam, IDataSource<NpcParam> npcParam)
    {
        this.logger = logger;
        this.npcParams = npcParam.GetAll().ToList();
        this.configuration = config.Value;
        this.areaScalingSpEffects =
            spEffectParam
                .GetAll()
                .Where(d => config.Value.Settings.ItemLotGeneratorSettings.ScannerAutoScalingSettings.AreaScalingSpEffectIds.Contains(d.ID))
                .ToList();

        this.vanillaScaleScores = [.. areaScalingSpEffects
            .Where(d => d.ID < 8000)
            .Select(d => d.maxHpRate)
            .Distinct()
            .OrderBy(d => d)];

        this.dlcScaleScores = [.. areaScalingSpEffects
            .Where(d => d.ID > 8000)
            .Select(d => d.maxHpRate)
            .Distinct()
            .OrderBy(d => d)];
    }

    public GameStage EvaluateDifficulty(ItemLotSettings settings, MSBE msb, List<NpcParam> relevantNpcs, string mapName, List<EventDropItemLotDetails> bossDropDetails)
    {
        // evalute difficulty and return game stage for the given map drops

        var npcs = msb.FilterRelevantNpcs(logger, relevantNpcs, mapName);

        // Average count of all npc difficulties in the map
        var regularEnemies = msb.Parts.Enemies
            .Where(d => !bossDropDetails.Any(s => s.EntityId == d.EntityID))
            .Where(d => npcs.Any(s => s.ID == d.NPCParamID))
            .DistinctBy(d => d.NPCParamID);

        var bossEnemies = msb.Parts.Enemies
            .Where(d => bossDropDetails.Any(s => s.EntityId == d.EntityID));

        var gameStages = regularEnemies
            .Select(d => new { ID = d.NPCParamID, GameStage = EvaluateDifficulty(settings, npcParams.Single(s => s.ID == d.NPCParamID)) });

        var bossGameStages = bossEnemies
            .Select(d => new { ID = d.NPCParamID, GameStage = EvaluateDifficulty(settings, npcParams.Single(s => s.ID == d.NPCParamID)) })
            .ToList();

        var averageDifficulty = 0.0;

        if (gameStages.Any())
        {
            averageDifficulty = gameStages.Average(d => (int)d.GameStage);
        }
        else if (bossGameStages.Any())
        {
            averageDifficulty = bossGameStages.Average(d => (int)d.GameStage);
        }

        GameStage averageGameStage = (GameStage)Math.Round(averageDifficulty);

        logger.LogInformation($"Map {mapName} evaluated average game stage of {averageGameStage}");

        return averageGameStage;
    }

    public GameStage EvaluateDifficulty(ItemLotSettings settings, NpcParam npc)
    {
        var (vanilla, dlc) = this.hpMultCache.GetOrAdd(settings.ID, InitializeHpMultMaps(settings));

        var maxHpMultiplier = 1.0f;
        var spEffects = npc.GenericParam.GetFieldNamesByFilter("spEffectID").Select(npc.GenericParam.GetValue<int>)
            .Where(d => areaScalingSpEffects.SingleOrDefault(s => s.ID == d) != null)
            .Select(d => areaScalingSpEffects.SingleOrDefault(s => s.ID == d))
            .ToList();

        if (!spEffects.Any())
        {
            logger.LogDebug($"NPC id {npc.ID} missing area scaling");
        }
        else
        {
            maxHpMultiplier = spEffects.Max(s => s.maxHpRate);
        }

        if (!vanilla.TryGetValue(maxHpMultiplier, out var goalRarity))
        {
            if (!dlc.TryGetValue(maxHpMultiplier, out goalRarity))
            {
                throw new Exception("Could not find goal rarity for hp multiplier {maxHpMultiplier}");
            }
        }

        foreach (var stage in settings.GameStageConfigs)
        {
            if (IntValueRange.CreateFrom(stage.AllowedRarities).Contains(goalRarity))
            {
                return stage.Stage;
            }
        }

        throw new Exception("Broken");
    }

    private (Dictionary<float, int> vanilla, Dictionary<float, int> dlc) InitializeHpMultMaps(ItemLotSettings settings)
    {
        var minRarity = settings.GameStageConfigs.Min(d => d.AllowedRarities.Min());
        var maxRarity = settings.GameStageConfigs.Max(d => d.AllowedRarities.Max());

        // TODO: config driven split?
        var hpMultToRarityMap = MathFunctions.MapToRange(
            this.vanillaScaleScores,
            minRarity,
            maxRarity);

        var dlcHpMultToRarityMap = MathFunctions.MapToRange(
            this.dlcScaleScores,
            minRarity + (maxRarity - minRarity) / 2,
            maxRarity);

        return (hpMultToRarityMap, dlcHpMultToRarityMap);
    }
}
