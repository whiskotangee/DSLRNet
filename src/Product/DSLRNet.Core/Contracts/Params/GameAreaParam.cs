using System;

public class GameAreaParam : ParamBase<GameAreaParam>
{
    public int ID { get { return this.GetValue<int>("ID"); } set { this.SetValue("ID", value); } }
    public string Name { get { return this.GetValue<string>("Name"); } set { this.SetValue("Name", value); } }
    public byte disableParam_NT { get { return this.GetValue<byte>("disableParam_NT"); } set { this.SetValue("disableParam_NT", value); } }
    public byte disableParamReserve1 { get { return this.GetValue<byte>("disableParamReserve1"); } set { this.SetValue("disableParamReserve1", value); } }
    public byte[] disableParamReserve2 { get { return this.GetValue<byte[]>("disableParamReserve2"); } set { this.SetValue("disableParamReserve2", value); } }
    public uint bonusSoul_single { get { return this.GetValue<uint>("bonusSoul_single"); } set { this.SetValue("bonusSoul_single", value); } }
    public uint bonusSoul_multi { get { return this.GetValue<uint>("bonusSoul_multi"); } set { this.SetValue("bonusSoul_multi", value); } }
    public uint humanityPointCountFlagIdTop { get { return this.GetValue<uint>("humanityPointCountFlagIdTop"); } set { this.SetValue("humanityPointCountFlagIdTop", value); } }
    public ushort humanityDropPoint1 { get { return this.GetValue<ushort>("humanityDropPoint1"); } set { this.SetValue("humanityDropPoint1", value); } }
    public ushort humanityDropPoint2 { get { return this.GetValue<ushort>("humanityDropPoint2"); } set { this.SetValue("humanityDropPoint2", value); } }
    public ushort humanityDropPoint3 { get { return this.GetValue<ushort>("humanityDropPoint3"); } set { this.SetValue("humanityDropPoint3", value); } }
    public ushort humanityDropPoint4 { get { return this.GetValue<ushort>("humanityDropPoint4"); } set { this.SetValue("humanityDropPoint4", value); } }
    public ushort humanityDropPoint5 { get { return this.GetValue<ushort>("humanityDropPoint5"); } set { this.SetValue("humanityDropPoint5", value); } }
    public ushort humanityDropPoint6 { get { return this.GetValue<ushort>("humanityDropPoint6"); } set { this.SetValue("humanityDropPoint6", value); } }
    public ushort humanityDropPoint7 { get { return this.GetValue<ushort>("humanityDropPoint7"); } set { this.SetValue("humanityDropPoint7", value); } }
    public ushort humanityDropPoint8 { get { return this.GetValue<ushort>("humanityDropPoint8"); } set { this.SetValue("humanityDropPoint8", value); } }
    public ushort humanityDropPoint9 { get { return this.GetValue<ushort>("humanityDropPoint9"); } set { this.SetValue("humanityDropPoint9", value); } }
    public ushort humanityDropPoint10 { get { return this.GetValue<ushort>("humanityDropPoint10"); } set { this.SetValue("humanityDropPoint10", value); } }
    public uint soloBreakInPoint_Min { get { return this.GetValue<uint>("soloBreakInPoint_Min"); } set { this.SetValue("soloBreakInPoint_Min", value); } }
    public uint soloBreakInPoint_Max { get { return this.GetValue<uint>("soloBreakInPoint_Max"); } set { this.SetValue("soloBreakInPoint_Max", value); } }
    public uint defeatBossFlagId_forSignAimList { get { return this.GetValue<uint>("defeatBossFlagId_forSignAimList"); } set { this.SetValue("defeatBossFlagId_forSignAimList", value); } }
    public uint displayAimFlagId { get { return this.GetValue<uint>("displayAimFlagId"); } set { this.SetValue("displayAimFlagId", value); } }
    public uint foundBossFlagId { get { return this.GetValue<uint>("foundBossFlagId"); } set { this.SetValue("foundBossFlagId", value); } }
    public int foundBossTextId { get { return this.GetValue<int>("foundBossTextId"); } set { this.SetValue("foundBossTextId", value); } }
    public int notFindBossTextId { get { return this.GetValue<int>("notFindBossTextId"); } set { this.SetValue("notFindBossTextId", value); } }
    public uint bossChallengeFlagId { get { return this.GetValue<uint>("bossChallengeFlagId"); } set { this.SetValue("bossChallengeFlagId", value); } }
    public uint defeatBossFlagId { get { return this.GetValue<uint>("defeatBossFlagId"); } set { this.SetValue("defeatBossFlagId", value); } }
    public float bossPosX { get { return this.GetValue<float>("bossPosX"); } set { this.SetValue("bossPosX", value); } }
    public float bossPosY { get { return this.GetValue<float>("bossPosY"); } set { this.SetValue("bossPosY", value); } }
    public float bossPosZ { get { return this.GetValue<float>("bossPosZ"); } set { this.SetValue("bossPosZ", value); } }
    public byte bossMapAreaNo { get { return this.GetValue<byte>("bossMapAreaNo"); } set { this.SetValue("bossMapAreaNo", value); } }
    public byte bossMapBlockNo { get { return this.GetValue<byte>("bossMapBlockNo"); } set { this.SetValue("bossMapBlockNo", value); } }
    public byte bossMapMapNo { get { return this.GetValue<byte>("bossMapMapNo"); } set { this.SetValue("bossMapMapNo", value); } }
    public byte[] reserve { get { return this.GetValue<byte[]>("reserve"); } set { this.SetValue("reserve", value); } }
}
