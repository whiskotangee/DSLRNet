namespace DSLRNet.Core.Scan;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using System.Collections.Concurrent;

public class DifficultyEvaluator
{
    private readonly ILogger<DifficultyEvaluator> logger;
    private readonly Dictionary<int, NpcParam> npcParams;
    private readonly Dictionary<int, SpEffectParam> allSpEffects;
    private readonly List<SpEffectParam> areaScalingSpEffects;
    private readonly List<SpEffectParam> vanillaSpEffects;
    private readonly List<SpEffectParam> dlcSpEffects;

    private readonly ConcurrentDictionary<int, (Dictionary<int, GameStage> vanilla, Dictionary<int, GameStage> dlc)> scaleCache = [];

    public DifficultyEvaluator(ILogger<DifficultyEvaluator> logger, IOptions<Configuration> config, DataAccess dataAccess)
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

    public void AssignBossGameStages(Dictionary<string, MSBE> maps, ItemLotSettings settings, List<EventDropItemLotDetails> lotDetails)
    {
        // pass through and compile all bosses across the game

        logger.LogInformation($"Compiling all boss hp ranges for more fine tuned rankings");
        Dictionary<GameStage, IntValueRange> gameStageHpRanges = [];

        IEnumerable<EventDropItemLotDetails> withEntityId = lotDetails.Where(d => d.EntityId > 0);
        foreach (var msb in maps)
        {
            logger.LogDebug($"Checking map {msb.Key}");
            foreach (EventDropItemLotDetails? details in withEntityId)
            {
                MSBE.Part.Enemy? foundEvent = msb.Value.Parts.Enemies.Where(d => d.EntityID == details.EntityId).FirstOrDefault();
                if (foundEvent != null)
                {
                    NpcParam foundNpc = npcParams[foundEvent.NPCParamID];
                    details.NpcId = foundNpc.ID;
                    details.NpcParam = foundNpc;
                    details.EvaluatedGameStage = EvaluateDifficultyByScalingSpEffect(settings, foundNpc);
                    details.FinalGameStage = details.EvaluatedGameStage;
                    details.TotalHp = Convert.ToInt32(this.allSpEffects[foundNpc.spEffectID3].maxHpRate * foundNpc.hp);

                    if (!gameStageHpRanges.TryGetValue(details.EvaluatedGameStage, out var value))
                    {
                        gameStageHpRanges[details.EvaluatedGameStage] = new IntValueRange((int)details.NpcParam.hp, (int)details.NpcParam.hp + 1);
                        value = gameStageHpRanges[details.EvaluatedGameStage];
                    }
                    else
                    {
                        value.Expand((int)details.NpcParam.hp);
                    }

                    logger.LogDebug($"Boss HP range for game stage {details.EvaluatedGameStage} is now {value}");
                }
            }
        }

        logger.LogInformation($"Bumping top 20% bosses by HP up a game stage to simulate 'better drops for harder bosses'");
        // with the hp ranges known per game stage based on spEffect scaling
        // We can fine tune and give a game stage bump to the top % hardest bosses in the map
        foreach (var gameStage in Enum.GetValues<GameStage>())
        {
            var hpRange = gameStageHpRanges[gameStage];
            var bosses = lotDetails.Where(d => d.EvaluatedGameStage == gameStage).ToList();
            var topBosses = bosses.Where(d => d.NpcParam != null).OrderByDescending(d => d.NpcParam.hp).Take((int)(bosses.Count * 0.2));
            foreach (var topBoss in topBosses)
            {
                var oldGameStage = topBoss.EvaluatedGameStage;
                topBoss.FinalGameStage = (GameStage)Math.Clamp((int)topBoss.EvaluatedGameStage + 1, (int)GameStage.Early, (int)GameStage.End);
                logger.LogDebug($"Boss {topBoss.NpcId}-{topBoss.NpcParam.Name} is being bumped from {oldGameStage} to {topBoss.FinalGameStage}");
            }
        }
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
            .Select(d => new { ID = d.NPCParamID, GameStage = EvaluateDifficultyByScalingSpEffect(settings, npcParams[d.NPCParamID]) });

        var bossGameStages = bossEnemies
            .Select(d => new { ID = d.NPCParamID, GameStage = EvaluateDifficultyByScalingSpEffect(settings, npcParams[d.NPCParamID]) })
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

    public GameStage EvaluateDifficultyByScalingSpEffect(ItemLotSettings settings, NpcParam npc)
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
                    throw new Exception("Literally can't find what scaling SPEffect {spEffectId} is");
                }
            }
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
