using System;

public class EquipParamAccessory : ParamBase<EquipParamAccessory>
{
    public int ID { get { return this.GenericParam.GetValue<int>("ID"); } set { this.GenericParam.SetValue("ID", value); } }
    public string Name { get { return this.GenericParam.GetValue<string>("Name"); } set { this.GenericParam.SetValue("Name", value); } }
    public byte disableParam_NT { get { return this.GenericParam.GetValue<byte>("disableParam_NT"); } set { this.GenericParam.SetValue("disableParam_NT", value); } }
    public byte disableParamReserve1 { get { return this.GenericParam.GetValue<byte>("disableParamReserve1"); } set { this.GenericParam.SetValue("disableParamReserve1", value); } }
    public byte[] disableParamReserve2 { get { return this.GenericParam.GetValue<byte[]>("disableParamReserve2"); } set { this.GenericParam.SetValue("disableParamReserve2", value); } }
    public int refId { get { return this.GenericParam.GetValue<int>("refId"); } set { this.GenericParam.SetValue("refId", value); } }
    public int sfxVariationId { get { return this.GenericParam.GetValue<int>("sfxVariationId"); } set { this.GenericParam.SetValue("sfxVariationId", value); } }
    public float weight { get { return this.GenericParam.GetValue<float>("weight"); } set { this.GenericParam.SetValue("weight", value); } }
    public int behaviorId { get { return this.GenericParam.GetValue<int>("behaviorId"); } set { this.GenericParam.SetValue("behaviorId", value); } }
    public int basicPrice { get { return this.GenericParam.GetValue<int>("basicPrice"); } set { this.GenericParam.SetValue("basicPrice", value); } }
    public int sellValue { get { return this.GenericParam.GetValue<int>("sellValue"); } set { this.GenericParam.SetValue("sellValue", value); } }
    public int sortId { get { return this.GenericParam.GetValue<int>("sortId"); } set { this.GenericParam.SetValue("sortId", value); } }
    public int qwcId { get { return this.GenericParam.GetValue<int>("qwcId"); } set { this.GenericParam.SetValue("qwcId", value); } }
    public ushort equipModelId { get { return this.GenericParam.GetValue<ushort>("equipModelId"); } set { this.GenericParam.SetValue("equipModelId", value); } }
    public ushort iconId { get { return this.GenericParam.GetValue<ushort>("iconId"); } set { this.GenericParam.SetValue("iconId", value); } }
    public short shopLv { get { return this.GenericParam.GetValue<short>("shopLv"); } set { this.GenericParam.SetValue("shopLv", value); } }
    public short trophySGradeId { get { return this.GenericParam.GetValue<short>("trophySGradeId"); } set { this.GenericParam.SetValue("trophySGradeId", value); } }
    public short trophySeqId { get { return this.GenericParam.GetValue<short>("trophySeqId"); } set { this.GenericParam.SetValue("trophySeqId", value); } }
    public byte equipModelCategory { get { return this.GenericParam.GetValue<byte>("equipModelCategory"); } set { this.GenericParam.SetValue("equipModelCategory", value); } }
    public byte equipModelGender { get { return this.GenericParam.GetValue<byte>("equipModelGender"); } set { this.GenericParam.SetValue("equipModelGender", value); } }
    public byte accessoryCategory { get { return this.GenericParam.GetValue<byte>("accessoryCategory"); } set { this.GenericParam.SetValue("accessoryCategory", value); } }
    public byte refCategory { get { return this.GenericParam.GetValue<byte>("refCategory"); } set { this.GenericParam.SetValue("refCategory", value); } }
    public byte spEffectCategory { get { return this.GenericParam.GetValue<byte>("spEffectCategory"); } set { this.GenericParam.SetValue("spEffectCategory", value); } }
    public byte sortGroupId { get { return this.GenericParam.GetValue<byte>("sortGroupId"); } set { this.GenericParam.SetValue("sortGroupId", value); } }
    public int vagrantItemLotId { get { return this.GenericParam.GetValue<int>("vagrantItemLotId"); } set { this.GenericParam.SetValue("vagrantItemLotId", value); } }
    public int vagrantBonusEneDropItemLotId { get { return this.GenericParam.GetValue<int>("vagrantBonusEneDropItemLotId"); } set { this.GenericParam.SetValue("vagrantBonusEneDropItemLotId", value); } }
    public int vagrantItemEneDropItemLotId { get { return this.GenericParam.GetValue<int>("vagrantItemEneDropItemLotId"); } set { this.GenericParam.SetValue("vagrantItemEneDropItemLotId", value); } }
    public byte isDeposit { get { return this.GenericParam.GetValue<byte>("isDeposit"); } set { this.GenericParam.SetValue("isDeposit", value); } }
    public byte isEquipOutBrake { get { return this.GenericParam.GetValue<byte>("isEquipOutBrake"); } set { this.GenericParam.SetValue("isEquipOutBrake", value); } }
    public byte disableMultiDropShare { get { return this.GenericParam.GetValue<byte>("disableMultiDropShare"); } set { this.GenericParam.SetValue("disableMultiDropShare", value); } }
    public byte isDiscard { get { return this.GenericParam.GetValue<byte>("isDiscard"); } set { this.GenericParam.SetValue("isDiscard", value); } }
    public byte isDrop { get { return this.GenericParam.GetValue<byte>("isDrop"); } set { this.GenericParam.SetValue("isDrop", value); } }
    public byte showLogCondType { get { return this.GenericParam.GetValue<byte>("showLogCondType"); } set { this.GenericParam.SetValue("showLogCondType", value); } }
    public byte showDialogCondType { get { return this.GenericParam.GetValue<byte>("showDialogCondType"); } set { this.GenericParam.SetValue("showDialogCondType", value); } }
    public byte rarity { get { return this.GenericParam.GetValue<byte>("rarity"); } set { this.GenericParam.SetValue("rarity", value); } }
    public byte[] pad2 { get { return this.GenericParam.GetValue<byte[]>("pad2"); } set { this.GenericParam.SetValue("pad2", value); } }
    public int saleValue { get { return this.GenericParam.GetValue<int>("saleValue"); } set { this.GenericParam.SetValue("saleValue", value); } }
    public short accessoryGroup { get { return this.GenericParam.GetValue<short>("accessoryGroup"); } set { this.GenericParam.SetValue("accessoryGroup", value); } }
    public byte[] pad3 { get { return this.GenericParam.GetValue<byte[]>("pad3"); } set { this.GenericParam.SetValue("pad3", value); } }
    public sbyte compTrophySedId { get { return this.GenericParam.GetValue<sbyte>("compTrophySedId"); } set { this.GenericParam.SetValue("compTrophySedId", value); } }
    public int residentSpEffectId1 { get { return this.GenericParam.GetValue<int>("residentSpEffectId1"); } set { this.GenericParam.SetValue("residentSpEffectId1", value); } }
    public int residentSpEffectId2 { get { return this.GenericParam.GetValue<int>("residentSpEffectId2"); } set { this.GenericParam.SetValue("residentSpEffectId2", value); } }
    public int residentSpEffectId3 { get { return this.GenericParam.GetValue<int>("residentSpEffectId3"); } set { this.GenericParam.SetValue("residentSpEffectId3", value); } }
    public int residentSpEffectId4 { get { return this.GenericParam.GetValue<int>("residentSpEffectId4"); } set { this.GenericParam.SetValue("residentSpEffectId4", value); } }
    public byte[] pad1 { get { return this.GenericParam.GetValue<byte[]>("pad1"); } set { this.GenericParam.SetValue("pad1", value); } }
}
