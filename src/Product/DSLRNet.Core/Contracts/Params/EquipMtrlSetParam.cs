namespace DSLRNet.Core.Contracts.Params;

using System;

public class EquipMtrlSetParam : ParamBase<EquipMtrlSetParam>
{
    public string Name { get { return this.GetValue<string>("Name"); } set { this.SetValue("Name", value); } }
    public int materialId01 { get { return this.GetValue<int>("materialId01"); } set { this.SetValue("materialId01", value); } }
    public int materialId02 { get { return this.GetValue<int>("materialId02"); } set { this.SetValue("materialId02", value); } }
    public int materialId03 { get { return this.GetValue<int>("materialId03"); } set { this.SetValue("materialId03", value); } }
    public int materialId04 { get { return this.GetValue<int>("materialId04"); } set { this.SetValue("materialId04", value); } }
    public int materialId05 { get { return this.GetValue<int>("materialId05"); } set { this.SetValue("materialId05", value); } }
    public int materialId06 { get { return this.GetValue<int>("materialId06"); } set { this.SetValue("materialId06", value); } }
    public byte[] pad_id { get { return this.GetValue<byte[]>("pad_id"); } set { this.SetValue("pad_id", value); } }
    public sbyte itemNum01 { get { return this.GetValue<sbyte>("itemNum01"); } set { this.SetValue("itemNum01", value); } }
    public sbyte itemNum02 { get { return this.GetValue<sbyte>("itemNum02"); } set { this.SetValue("itemNum02", value); } }
    public sbyte itemNum03 { get { return this.GetValue<sbyte>("itemNum03"); } set { this.SetValue("itemNum03", value); } }
    public sbyte itemNum04 { get { return this.GetValue<sbyte>("itemNum04"); } set { this.SetValue("itemNum04", value); } }
    public sbyte itemNum05 { get { return this.GetValue<sbyte>("itemNum05"); } set { this.SetValue("itemNum05", value); } }
    public sbyte itemNum06 { get { return this.GetValue<sbyte>("itemNum06"); } set { this.SetValue("itemNum06", value); } }
    public byte[] pad_num { get { return this.GetValue<byte[]>("pad_num"); } set { this.SetValue("pad_num", value); } }
    public byte materialCate01 { get { return this.GetValue<byte>("materialCate01"); } set { this.SetValue("materialCate01", value); } }
    public byte materialCate02 { get { return this.GetValue<byte>("materialCate02"); } set { this.SetValue("materialCate02", value); } }
    public byte materialCate03 { get { return this.GetValue<byte>("materialCate03"); } set { this.SetValue("materialCate03", value); } }
    public byte materialCate04 { get { return this.GetValue<byte>("materialCate04"); } set { this.SetValue("materialCate04", value); } }
    public byte materialCate05 { get { return this.GetValue<byte>("materialCate05"); } set { this.SetValue("materialCate05", value); } }
    public byte materialCate06 { get { return this.GetValue<byte>("materialCate06"); } set { this.SetValue("materialCate06", value); } }
    public byte[] pad_cate { get { return this.GetValue<byte[]>("pad_cate"); } set { this.SetValue("pad_cate", value); } }
    public byte isDisableDispNum01 { get { return this.GetValue<byte>("isDisableDispNum01"); } set { this.SetValue("isDisableDispNum01", value); } }
    public byte isDisableDispNum02 { get { return this.GetValue<byte>("isDisableDispNum02"); } set { this.SetValue("isDisableDispNum02", value); } }
    public byte isDisableDispNum03 { get { return this.GetValue<byte>("isDisableDispNum03"); } set { this.SetValue("isDisableDispNum03", value); } }
    public byte isDisableDispNum04 { get { return this.GetValue<byte>("isDisableDispNum04"); } set { this.SetValue("isDisableDispNum04", value); } }
    public byte isDisableDispNum05 { get { return this.GetValue<byte>("isDisableDispNum05"); } set { this.SetValue("isDisableDispNum05", value); } }
    public byte isDisableDispNum06 { get { return this.GetValue<byte>("isDisableDispNum06"); } set { this.SetValue("isDisableDispNum06", value); } }
    public byte[] pad { get { return this.GetValue<byte[]>("pad"); } set { this.SetValue("pad", value); } }
}
