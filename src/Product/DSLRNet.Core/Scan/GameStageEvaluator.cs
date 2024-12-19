namespace DSLRNet.Core.Scan;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using System.Collections.Concurrent;

public class GameStageEvaluator
{
    private readonly ILogger<GameStageEvaluator> logger;
    private readonly List<NpcParam> npcParams;
    private readonly Configuration configuration;
    private readonly List<SpEffectParam> allSpEffects;
    private readonly List<SpEffectParam> areaScalingSpEffects;

    private readonly List<SpEffectParam> vanillaSpEffects;
    private readonly List<SpEffectParam> dlcSpEffects;

    private ConcurrentDictionary<int, (Dictionary<int, GameStage> vanilla, Dictionary<int, GameStage> dlc)> scaleCache = [];

    public GameStageEvaluator(ILogger<GameStageEvaluator> logger, IOptions<Configuration> config, DataAccess dataAccess)
    {
        this.logger = logger;
        this.allSpEffects = dataAccess.SpEffectParam.GetAll().ToList();
        this.npcParams = dataAccess.NpcParam.GetAll().ToList();
        this.configuration = config.Value;
        this.areaScalingSpEffects =
            this.allSpEffects
                .Where(d => config.Value.ScannerAutoScalingSettings.AreaScalingSpEffectIds.Contains(d.ID))
                .ToList();

        this.vanillaSpEffects = [.. areaScalingSpEffects
            .Where(d => d.ID < 8000)
            .ToList()];

        this.dlcSpEffects = [.. areaScalingSpEffects
            .Where(d => d.ID > 8000)
            .ToList()];

    }

    public GameStage EvaluateDifficulty(ItemLotSettings settings, MSBE msb, List<NpcParam> relevantNpcs, string mapName, List<EventDropItemLotDetails> bossDropDetails)
    {
        // evalute difficulty and return game stage for the given map drops

        var npcs = msb.FilterRelevantNpcs(logger, relevantNpcs, Path.GetFileName(mapName));

        // Average count of all npc difficulties in the map
        var regularEnemies = msb.Parts.Enemies
            .Where(d => !bossDropDetails.Any(s => s.EntityId == d.EntityID))
            .Where(d => npcs.Any(s => s.ID == d.NPCParamID))
            .DistinctBy(d => d.NPCParamID);

        var bossEnemies = msb.Parts.Enemies
            .Where(d => bossDropDetails.Any(s => s.EntityId == d.EntityID));

        var gameStages = regularEnemies
            .Select(d => new { ID = d.NPCParamID, GameStage = EvaluateDifficulty(settings, npcParams.Single(s => s.ID == d.NPCParamID), false) });

        var bossGameStages = bossEnemies
            .Select(d => new { ID = d.NPCParamID, GameStage = EvaluateDifficulty(settings, npcParams.Single(s => s.ID == d.NPCParamID), true) })
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

    public GameStage EvaluateDifficulty(ItemLotSettings settings, NpcParam npc, bool isBoss)
    {
        var (vanilla, dlc) = this.scaleCache.GetOrAdd(settings.ID, InitializeHpMultMaps(settings));

        var spEffectId = npc.spEffectID3;
        var gameStage = GameStage.Early;

        if (spEffectId <= 0)
        {
            logger.LogDebug($"NPC id {npc.ID} missing area scaling");
        }
        else
        {
            var spEffect = this.allSpEffects.Single(d => d.ID == spEffectId);

            if (!vanilla.TryGetValue(spEffect.ID, out gameStage) && !dlc.TryGetValue(spEffect.ID, out gameStage))
            {
                // could be a custom scaling spEffect - try that
                var nearestHpMultiplier = MathFunctions.GetNearestValue(spEffect.maxHpRate, areaScalingSpEffects.Select(d => d.maxHpRate));

                var areaScalingId = areaScalingSpEffects.First(d => d.maxHpRate == nearestHpMultiplier).ID;

                if (!vanilla.TryGetValue(areaScalingId, out gameStage) && !dlc.TryGetValue(areaScalingId, out gameStage))
                {
                    throw new Exception("Literally can't find what the hell scaling SPEffect {spEffectId} is");
                }
            }

            if (isBoss)
            {
                var totalHp = npc.hp * (spEffect?.maxHpRate ?? 1.0f);

                gameStage = (GameStage)Math.Clamp((int)gameStage + 1, (int)GameStage.Early, (int)GameStage.End);
                logger.LogInformation($"Boss {npc.Name} of Id {npc.ID} with total hp {totalHp:F2} returned game stage {gameStage}");
            }
        }

        return gameStage;
    }

    private (Dictionary<int, GameStage> vanilla, Dictionary<int, GameStage> dlc) InitializeHpMultMaps(ItemLotSettings settings)
    {
        // TODO: config driven split?
        Dictionary<int, int> hpMultToRarityMap = MathFunctions.MapToRange(
            this.vanillaSpEffects,
            (spEffect) => spEffect.maxHpRate,
            (spEffect) => spEffect.ID,
            (int)settings.GameStageConfigs.Min(d => d.Stage),
            (int)settings.GameStageConfigs.Max(d => d.Stage));

        Dictionary<int, int> dlcHpMultToRarityMap = MathFunctions.MapToRange(
            this.dlcSpEffects.ToList(),
            (spEffect) => spEffect.maxHpRate,
            (spEffect) => spEffect.ID,
            (int)GameStage.Late,
            (int)GameStage.End);


        return (hpMultToRarityMap.ToDictionary(k => k.Key, v => (GameStage)v.Value), dlcHpMultToRarityMap.ToDictionary(k => k.Key, v => (GameStage)v.Value));
    }
}
