
namespace DSLRNet.Core.Contracts.Params;

public partial class EquipParamAccessory : ParamBase<EquipParamAccessory>
{
    public int ID { get { return this.GenericParam.GetValue<int>("ID"); } set { this.GenericParam.SetValue("ID", value); } }
    public string Name { get { return this.GenericParam.GetValue<string>("Name"); } set { this.GenericParam.SetValue("Name", value); } }
    public int disableParam_NT { get { return this.GenericParam.GetValue<int>("disableParam_NT"); } set { this.GenericParam.SetValue("disableParam_NT", value); } }
    public int disableParamReserve1 { get { return this.GenericParam.GetValue<int>("disableParamReserve1"); } set { this.GenericParam.SetValue("disableParamReserve1", value); } }
    public string disableParamReserve2 { get { return this.GenericParam.GetValue<string>("disableParamReserve2"); } set { this.GenericParam.SetValue("disableParamReserve2", value); } }
    public int refId { get { return this.GenericParam.GetValue<int>("refId"); } set { this.GenericParam.SetValue("refId", value); } }
    public int sfxVariationId { get { return this.GenericParam.GetValue<int>("sfxVariationId"); } set { this.GenericParam.SetValue("sfxVariationId", value); } }
    public float weight { get { return this.GenericParam.GetValue<float>("weight"); } set { this.GenericParam.SetValue("weight", value); } }
    public int behaviorId { get { return this.GenericParam.GetValue<int>("behaviorId"); } set { this.GenericParam.SetValue("behaviorId", value); } }
    public int basicPrice { get { return this.GenericParam.GetValue<int>("basicPrice"); } set { this.GenericParam.SetValue("basicPrice", value); } }
    public int sellValue { get { return this.GenericParam.GetValue<int>("sellValue"); } set { this.GenericParam.SetValue("sellValue", value); } }
    public int sortId { get { return this.GenericParam.GetValue<int>("sortId"); } set { this.GenericParam.SetValue("sortId", value); } }
    public int qwcId { get { return this.GenericParam.GetValue<int>("qwcId"); } set { this.GenericParam.SetValue("qwcId", value); } }
    public int equipModelId { get { return this.GenericParam.GetValue<int>("equipModelId"); } set { this.GenericParam.SetValue("equipModelId", value); } }
    public int iconId { get { return this.GenericParam.GetValue<int>("iconId"); } set { this.GenericParam.SetValue("iconId", value); } }
    public int shopLv { get { return this.GenericParam.GetValue<int>("shopLv"); } set { this.GenericParam.SetValue("shopLv", value); } }
    public int trophySGradeId { get { return this.GenericParam.GetValue<int>("trophySGradeId"); } set { this.GenericParam.SetValue("trophySGradeId", value); } }
    public int trophySeqId { get { return this.GenericParam.GetValue<int>("trophySeqId"); } set { this.GenericParam.SetValue("trophySeqId", value); } }
    public int equipModelCategory { get { return this.GenericParam.GetValue<int>("equipModelCategory"); } set { this.GenericParam.SetValue("equipModelCategory", value); } }
    public int equipModelGender { get { return this.GenericParam.GetValue<int>("equipModelGender"); } set { this.GenericParam.SetValue("equipModelGender", value); } }
    public int accessoryCategory { get { return this.GenericParam.GetValue<int>("accessoryCategory"); } set { this.GenericParam.SetValue("accessoryCategory", value); } }
    public int refCategory { get { return this.GenericParam.GetValue<int>("refCategory"); } set { this.GenericParam.SetValue("refCategory", value); } }
    public int spEffectCategory { get { return this.GenericParam.GetValue<int>("spEffectCategory"); } set { this.GenericParam.SetValue("spEffectCategory", value); } }
    public int sortGroupId { get { return this.GenericParam.GetValue<int>("sortGroupId"); } set { this.GenericParam.SetValue("sortGroupId", value); } }
    public int vagrantItemLotId { get { return this.GenericParam.GetValue<int>("vagrantItemLotId"); } set { this.GenericParam.SetValue("vagrantItemLotId", value); } }
    public int vagrantBonusEneDropItemLotId { get { return this.GenericParam.GetValue<int>("vagrantBonusEneDropItemLotId"); } set { this.GenericParam.SetValue("vagrantBonusEneDropItemLotId", value); } }
    public int vagrantItemEneDropItemLotId { get { return this.GenericParam.GetValue<int>("vagrantItemEneDropItemLotId"); } set { this.GenericParam.SetValue("vagrantItemEneDropItemLotId", value); } }
    public int isDeposit { get { return this.GenericParam.GetValue<int>("isDeposit"); } set { this.GenericParam.SetValue("isDeposit", value); } }
    public int isEquipOutBrake { get { return this.GenericParam.GetValue<int>("isEquipOutBrake"); } set { this.GenericParam.SetValue("isEquipOutBrake", value); } }
    public int disableMultiDropShare { get { return this.GenericParam.GetValue<int>("disableMultiDropShare"); } set { this.GenericParam.SetValue("disableMultiDropShare", value); } }
    public int isDiscard { get { return this.GenericParam.GetValue<int>("isDiscard"); } set { this.GenericParam.SetValue("isDiscard", value); } }
    public int isDrop { get { return this.GenericParam.GetValue<int>("isDrop"); } set { this.GenericParam.SetValue("isDrop", value); } }
    public int showLogCondType { get { return this.GenericParam.GetValue<int>("showLogCondType"); } set { this.GenericParam.SetValue("showLogCondType", value); } }
    public int showDialogCondType { get { return this.GenericParam.GetValue<int>("showDialogCondType"); } set { this.GenericParam.SetValue("showDialogCondType", value); } }
    public int rarity { get { return this.GenericParam.GetValue<int>("rarity"); } set { this.GenericParam.SetValue("rarity", value); } }
    public string pad2 { get { return this.GenericParam.GetValue<string>("pad2"); } set { this.GenericParam.SetValue("pad2", value); } }
    public int saleValue { get { return this.GenericParam.GetValue<int>("saleValue"); } set { this.GenericParam.SetValue("saleValue", value); } }
    public int accessoryGroup { get { return this.GenericParam.GetValue<int>("accessoryGroup"); } set { this.GenericParam.SetValue("accessoryGroup", value); } }
    public int pad3 { get { return this.GenericParam.GetValue<int>("pad3"); } set { this.GenericParam.SetValue("pad3", value); } }
    public int compTrophySedId { get { return this.GenericParam.GetValue<int>("compTrophySedId"); } set { this.GenericParam.SetValue("compTrophySedId", value); } }
    public int residentSpEffectId1 { get { return this.GenericParam.GetValue<int>("residentSpEffectId1"); } set { this.GenericParam.SetValue("residentSpEffectId1", value); } }
    public int residentSpEffectId2 { get { return this.GenericParam.GetValue<int>("residentSpEffectId2"); } set { this.GenericParam.SetValue("residentSpEffectId2", value); } }
    public int residentSpEffectId3 { get { return this.GenericParam.GetValue<int>("residentSpEffectId3"); } set { this.GenericParam.SetValue("residentSpEffectId3", value); } }
    public int residentSpEffectId4 { get { return this.GenericParam.GetValue<int>("residentSpEffectId4"); } set { this.GenericParam.SetValue("residentSpEffectId4", value); } }
    public string pad1 { get { return this.GenericParam.GetValue<string>("pad1"); } set { this.GenericParam.SetValue("pad1", value); } }
}