namespace DSLRNet.Core.Handlers;

public class AshofWarHandler(
    RandomNumberGetter random, 
    IOptions<AshOfWarConfig> ashofWarConfig, 
    ParamEditsRepository generatedDataRepository,
    IDataSource<EquipParamGem> gemParamDataSource) : BaseHandler(generatedDataRepository)
{
    private IEnumerable<EquipParamGem> equipParamGems = gemParamDataSource.GetAll();
    private readonly AshOfWarConfig ashOfWarConfig = ashofWarConfig.Value;

    public void AssignAshOfWar(EquipParamWeapon weapon)
    {
        int weaponWmc = weapon.wepmotionCategory;

        // get sword artsId from set of equip gems compatible with this weapon
        var weaponType = weapon.wepType;
        string? boolFlagToCheck = ashOfWarConfig.WeaponTypeToCanMountWepFlags.FirstOrDefault(d => d.Id == weaponType)?.FlagName;

        var validGems = equipParamGems.Where(d => Convert.ToInt64(d.GetType().GetProperty(boolFlagToCheck).GetValue(d)) == 1).ToList();

        if (validGems.Any())
        {
            var chosenGem = random.GetRandomItem(validGems);
            int finalId = chosenGem.swordArtsParamId;

            weapon.swordArtsParamId = finalId;
        }
        else
        {
            Log.Logger.Warning($"Weapon Base {weaponType} named {weapon.Name} did not have any matching valid gems");
        }
    }
}

