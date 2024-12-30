namespace DSLRNet.Core.Contracts.Params;

using System;

public class EquipParamAccessory : ParamBase<EquipParamAccessory>
{
    public string Name { get { return this.GetValue<string>("Name"); } set { this.SetValue("Name", value); } }
    public byte disableParam_NT { get { return this.GetValue<byte>("disableParam_NT"); } set { this.SetValue("disableParam_NT", value); } }
    public byte disableParamReserve1 { get { return this.GetValue<byte>("disableParamReserve1"); } set { this.SetValue("disableParamReserve1", value); } }
    public Byte[] disableParamReserve2 { get { return this.GetValue<Byte[]>("disableParamReserve2"); } set { this.SetValue("disableParamReserve2", value); } }
    public int refId { get { return this.GetValue<int>("refId"); } set { this.SetValue("refId", value); } }
    public int sfxVariationId { get { return this.GetValue<int>("sfxVariationId"); } set { this.SetValue("sfxVariationId", value); } }
    public float weight { get { return this.GetValue<float>("weight"); } set { this.SetValue("weight", value); } }
    public int behaviorId { get { return this.GetValue<int>("behaviorId"); } set { this.SetValue("behaviorId", value); } }
    public int basicPrice { get { return this.GetValue<int>("basicPrice"); } set { this.SetValue("basicPrice", value); } }
    public int sellValue { get { return this.GetValue<int>("sellValue"); } set { this.SetValue("sellValue", value); } }
    public int sortId { get { return this.GetValue<int>("sortId"); } set { this.SetValue("sortId", value); } }
    public int qwcId { get { return this.GetValue<int>("qwcId"); } set { this.SetValue("qwcId", value); } }
    public ushort equipModelId { get { return this.GetValue<ushort>("equipModelId"); } set { this.SetValue("equipModelId", value); } }
    public ushort iconId { get { return this.GetValue<ushort>("iconId"); } set { this.SetValue("iconId", value); } }
    public short shopLv { get { return this.GetValue<short>("shopLv"); } set { this.SetValue("shopLv", value); } }
    public short trophySGradeId { get { return this.GetValue<short>("trophySGradeId"); } set { this.SetValue("trophySGradeId", value); } }
    public short trophySeqId { get { return this.GetValue<short>("trophySeqId"); } set { this.SetValue("trophySeqId", value); } }
    public byte equipModelCategory { get { return this.GetValue<byte>("equipModelCategory"); } set { this.SetValue("equipModelCategory", value); } }
    public byte equipModelGender { get { return this.GetValue<byte>("equipModelGender"); } set { this.SetValue("equipModelGender", value); } }
    public byte accessoryCategory { get { return this.GetValue<byte>("accessoryCategory"); } set { this.SetValue("accessoryCategory", value); } }
    public byte refCategory { get { return this.GetValue<byte>("refCategory"); } set { this.SetValue("refCategory", value); } }
    public byte spEffectCategory { get { return this.GetValue<byte>("spEffectCategory"); } set { this.SetValue("spEffectCategory", value); } }
    public byte sortGroupId { get { return this.GetValue<byte>("sortGroupId"); } set { this.SetValue("sortGroupId", value); } }
    public int vagrantItemLotId { get { return this.GetValue<int>("vagrantItemLotId"); } set { this.SetValue("vagrantItemLotId", value); } }
    public int vagrantBonusEneDropItemLotId { get { return this.GetValue<int>("vagrantBonusEneDropItemLotId"); } set { this.SetValue("vagrantBonusEneDropItemLotId", value); } }
    public int vagrantItemEneDropItemLotId { get { return this.GetValue<int>("vagrantItemEneDropItemLotId"); } set { this.SetValue("vagrantItemEneDropItemLotId", value); } }
    public byte isDeposit { get { return this.GetValue<byte>("isDeposit"); } set { this.SetValue("isDeposit", value); } }
    public byte isEquipOutBrake { get { return this.GetValue<byte>("isEquipOutBrake"); } set { this.SetValue("isEquipOutBrake", value); } }
    public byte disableMultiDropShare { get { return this.GetValue<byte>("disableMultiDropShare"); } set { this.SetValue("disableMultiDropShare", value); } }
    public byte isDiscard { get { return this.GetValue<byte>("isDiscard"); } set { this.SetValue("isDiscard", value); } }
    public byte isDrop { get { return this.GetValue<byte>("isDrop"); } set { this.SetValue("isDrop", value); } }
    public byte showLogCondType { get { return this.GetValue<byte>("showLogCondType"); } set { this.SetValue("showLogCondType", value); } }
    public byte showDialogCondType { get { return this.GetValue<byte>("showDialogCondType"); } set { this.SetValue("showDialogCondType", value); } }
    public byte rarity { get { return this.GetValue<byte>("rarity"); } set { this.SetValue("rarity", value); } }
    public Byte[] pad2 { get { return this.GetValue<Byte[]>("pad2"); } set { this.SetValue("pad2", value); } }
    public int saleValue { get { return this.GetValue<int>("saleValue"); } set { this.SetValue("saleValue", value); } }
    public short accessoryGroup { get { return this.GetValue<short>("accessoryGroup"); } set { this.SetValue("accessoryGroup", value); } }
    public Byte[] pad3 { get { return this.GetValue<Byte[]>("pad3"); } set { this.SetValue("pad3", value); } }
    public sbyte compTrophySedId { get { return this.GetValue<sbyte>("compTrophySedId"); } set { this.SetValue("compTrophySedId", value); } }
    public int residentSpEffectId1 { get { return this.GetValue<int>("residentSpEffectId1"); } set { this.SetValue("residentSpEffectId1", value); } }
    public int residentSpEffectId2 { get { return this.GetValue<int>("residentSpEffectId2"); } set { this.SetValue("residentSpEffectId2", value); } }
    public int residentSpEffectId3 { get { return this.GetValue<int>("residentSpEffectId3"); } set { this.SetValue("residentSpEffectId3", value); } }
    public int residentSpEffectId4 { get { return this.GetValue<int>("residentSpEffectId4"); } set { this.SetValue("residentSpEffectId4", value); } }
    public Byte[] pad1 { get { return this.GetValue<Byte[]>("pad1"); } set { this.SetValue("pad1", value); } }
}
