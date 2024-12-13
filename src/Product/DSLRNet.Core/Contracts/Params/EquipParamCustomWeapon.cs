using System;

public class EquipParamCustomWeapon : ParamBase<EquipParamCustomWeapon>
{
    public int ID { get { return this.GenericParam.GetValue<int>("ID"); } set { this.GenericParam.SetValue("ID", value); } }
    public string Name { get { return this.GenericParam.GetValue<string>("Name"); } set { this.GenericParam.SetValue("Name", value); } }
    public int baseWepId { get { return this.GenericParam.GetValue<int>("baseWepId"); } set { this.GenericParam.SetValue("baseWepId", value); } }
    public int gemId { get { return this.GenericParam.GetValue<int>("gemId"); } set { this.GenericParam.SetValue("gemId", value); } }
    public byte reinforceLv { get { return this.GenericParam.GetValue<byte>("reinforceLv"); } set { this.GenericParam.SetValue("reinforceLv", value); } }
    public byte[] pad { get { return this.GenericParam.GetValue<byte[]>("pad"); } set { this.GenericParam.SetValue("pad", value); } }
}
