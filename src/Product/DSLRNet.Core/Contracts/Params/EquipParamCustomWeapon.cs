public class EquipParamCustomWeapon : ParamBase<EquipParamCustomWeapon>
{
    public string Name { get { return this.GetValue<string>("Name"); } set { this.SetValue("Name", value); } }
    public int baseWepId { get { return this.GetValue<int>("baseWepId"); } set { this.SetValue("baseWepId", value); } }
    public int gemId { get { return this.GetValue<int>("gemId"); } set { this.SetValue("gemId", value); } }
    public byte reinforceLv { get { return this.GetValue<byte>("reinforceLv"); } set { this.SetValue("reinforceLv", value); } }
    public Byte[] pad { get { return this.GetValue<Byte[]>("pad"); } set { this.SetValue("pad", value); } }
}
