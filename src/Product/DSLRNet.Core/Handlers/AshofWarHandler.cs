namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.DAL;

public class AshofWarHandler(
    RandomProvider random,
    IOptions<Configuration> configuration,
    ParamEditsRepository generatedDataRepository,
    DataAccess dataAccess,
    ILogger<AshofWarHandler> logger) : BaseHandler(generatedDataRepository)
{
    private readonly IEnumerable<EquipParamGem> equipParamGems = dataAccess.EquipParamGem.GetAll();
    private readonly AshOfWarConfig ashOfWarConfig = configuration.Value.AshOfWarConfig;
    private readonly ILogger<AshofWarHandler> logger = logger;

    public void AssignAshOfWar(EquipParamWeapon weapon)
    {
        int weaponWmc = weapon.wepmotionCategory;

        // get sword artsId from set of equip gems compatible with this weapon
        int weaponType = weapon.wepType;
        string? boolFlagToCheck = this.ashOfWarConfig.WeaponTypeCanMountWepFlags.First(d => d.Id == weaponType).FlagName;

        List<EquipParamGem> validGems = this.equipParamGems.Where(d => d.GetValue<int>(boolFlagToCheck) == 1).ToList();

        if (validGems.Any())
        {
            EquipParamGem chosenGem = random.GetRandomItem(validGems);
            int finalId = chosenGem.swordArtsParamId;

            weapon.swordArtsParamId = finalId;
        }
        else
        {
            this.logger.LogWarning($"Weapon Base {weaponType} named {weapon.Name} did not have any valid gems to assign.");
        }
    }
}

