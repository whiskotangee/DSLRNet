namespace DSLRNet.Core.Scan;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using System.Collections.Concurrent;

public class GameStageEvaluator
{
    private readonly ILogger<GameStageEvaluator> logger;
    private readonly Dictionary<int, NpcParam> npcParams;
    private readonly Dictionary<int, SpEffectParam> allSpEffects;
    private readonly List<SpEffectParam> areaScalingSpEffects;
    private readonly List<SpEffectParam> vanillaSpEffects;
    private readonly List<SpEffectParam> dlcSpEffects;

    private readonly ConcurrentDictionary<int, (Dictionary<int, GameStage> vanilla, Dictionary<int, GameStage> dlc)> scaleCache = [];

    public GameStageEvaluator(ILogger<GameStageEvaluator> logger, IOptions<Configuration> config, DataAccess dataAccess)
    {
        this.logger = logger;
        this.allSpEffects = dataAccess.SpEffectParam.GetAll().ToDictionary(k => k.ID, v => v);
        this.npcParams = dataAccess.NpcParam.GetAll().ToDictionary(k => k.ID, v => v);
        this.areaScalingSpEffects =
            this.allSpEffects.Values
                .Where(d => config.Value.ScannerConfig.AreaScalingSpEffectIds.Contains(d.ID))
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

        IEnumerable<MSBE.Part.Enemy> regularEnemies = msb.Parts.Enemies
            .Where(d => !bossDropDetails.Any(s => s.EntityId == d.EntityID))
            .Where(d => relevantNpcs.Any(s => s.ID == d.NPCParamID))
            .DistinctBy(d => d.NPCParamID);

        IEnumerable<MSBE.Part.Enemy> bossEnemies = msb.Parts.Enemies
            .Where(d => bossDropDetails.Any(s => s.EntityId == d.EntityID));

        var gameStages = regularEnemies
            .Select(d => new { ID = d.NPCParamID, GameStage = EvaluateDifficulty(settings, npcParams[d.NPCParamID], false) });

        var bossGameStages = bossEnemies
            .Select(d => new { ID = d.NPCParamID, GameStage = EvaluateDifficulty(settings, npcParams[d.NPCParamID], true) })
            .ToList();

        double averageDifficulty = 0.0;

        if (gameStages.Any())
        {
            averageDifficulty = gameStages.Average(d => (int)d.GameStage);
        }
        else if (bossGameStages.Count != 0)
        {
            averageDifficulty = bossGameStages.Average(d => (int)d.GameStage);
        }

        GameStage averageGameStage = (GameStage)Math.Round(averageDifficulty);

        logger.LogInformation($"Map {mapName} evaluated average game stage of {averageGameStage}");

        return averageGameStage;
    }

    public GameStage EvaluateDifficulty(ItemLotSettings settings, NpcParam npc, bool isBoss)
    {
        (Dictionary<int, GameStage> vanilla, Dictionary<int, GameStage> dlc) = this.scaleCache.GetOrAdd(settings.ID, InitializeHpMultMaps(settings));

        int spEffectId = npc.spEffectID3;
        GameStage gameStage = GameStage.Early;

        if (spEffectId <= 0)
        {
            logger.LogDebug($"NPC id {npc.ID} missing area scaling");
        }
        else
        {
            SpEffectParam spEffect = this.allSpEffects[spEffectId];

            if (!vanilla.TryGetValue(spEffect.ID, out gameStage) && !dlc.TryGetValue(spEffect.ID, out gameStage))
            {
                // could be a custom scaling spEffect - try that
                float nearestHpMultiplier = MathFunctions.GetNearestValue(spEffect.maxHpRate, areaScalingSpEffects.Select(d => d.maxHpRate));

                int areaScalingId = areaScalingSpEffects.First(d => d.maxHpRate == nearestHpMultiplier).ID;

                if (!vanilla.TryGetValue(areaScalingId, out gameStage) && !dlc.TryGetValue(areaScalingId, out gameStage))
                {
                    throw new Exception("Literally can't find what the hell scaling SPEffect {spEffectId} is");
                }
            }

            //if (isBoss)
            //{
            //    float totalHp = npc.hp * (spEffect?.maxHpRate ?? 1.0f);

            //    gameStage = (GameStage)Math.Clamp((int)gameStage + 1, (int)GameStage.Early, (int)GameStage.End);
            //    logger.LogInformation($"Boss {npc.Name} of Id {npc.ID} with total hp {totalHp:F2} returned game stage {gameStage}");
            //}
        }

        return gameStage;
    }

    private (Dictionary<int, GameStage> vanilla, Dictionary<int, GameStage> dlc) InitializeHpMultMaps(ItemLotSettings settings)
    {
        Dictionary<int, int> hpMultToRarityMap = MathFunctions.MapToRange(
            this.vanillaSpEffects,
            (spEffect) => spEffect.maxHpRate,
            (spEffect) => spEffect.ID,
            (int)settings.GameStageConfigs.Values.Min(d => d.Stage),
            (int)settings.GameStageConfigs.Values.Max(d => d.Stage));

        Dictionary<int, int> dlcHpMultToRarityMap = MathFunctions.MapToRange(
            this.dlcSpEffects.ToList(),
            (spEffect) => spEffect.maxHpRate,
            (spEffect) => spEffect.ID,
            (int)GameStage.Late,
            (int)GameStage.End);


        return (hpMultToRarityMap.ToDictionary(k => k.Key, v => (GameStage)v.Value), dlcHpMultToRarityMap.ToDictionary(k => k.Key, v => (GameStage)v.Value));
    }
}
