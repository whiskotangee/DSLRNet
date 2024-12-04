namespace DSLRNet.Core.Handlers;

public class AshofWarHandler(
    RandomProvider random,
    IOptions<AshOfWarConfig> ashofWarConfig,
    ParamEditsRepository generatedDataRepository,
    IDataSource<EquipParamGem> gemParamDataSource) : BaseHandler(generatedDataRepository)
{
    private readonly IEnumerable<EquipParamGem> equipParamGems = gemParamDataSource.GetAll();
    private readonly AshOfWarConfig ashOfWarConfig = ashofWarConfig.Value;

    public void AssignAshOfWar(EquipParamWeapon weapon)
    {
        int weaponWmc = weapon.wepmotionCategory;

        // get sword artsId from set of equip gems compatible with this weapon
        int weaponType = weapon.wepType;
        string? boolFlagToCheck = this.ashOfWarConfig.WeaponTypeCanMountWepFlags.FirstOrDefault(d => d.Id == weaponType)?.FlagName;

        List<EquipParamGem> validGems = this.equipParamGems.Where(d => d.GenericParam.GetValue<int>(boolFlagToCheck) == 1).ToList();

        if (validGems.Any())
        {
            EquipParamGem chosenGem = random.GetRandomItem(validGems);
            int finalId = chosenGem.swordArtsParamId;

            weapon.swordArtsParamId = finalId;
        }
        else
        {
            Log.Logger.Warning($"Weapon Base {weaponType} named {weapon.Name} did not have any matching valid gems");
        }
    }
}

