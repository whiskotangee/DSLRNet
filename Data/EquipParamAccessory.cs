namespace DSLRNet.Data;

public class EquipParamAccessory
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int DisableParam_NT { get; set; }
    public int DisableParamReserve1 { get; set; }
    public string DisableParamReserve2 { get; set; } // Assuming this is a string due to the format [0|0|0]
    public int RefId { get; set; }
    public int SfxVariationId { get; set; }
    public double Weight { get; set; }
    public int BehaviorId { get; set; }
    public int BasicPrice { get; set; }
    public int SellValue { get; set; }
    public int SortId { get; set; }
    public int QwcId { get; set; }
    public int EquipModelId { get; set; }
    public int IconId { get; set; }
    public int ShopLv { get; set; }
    public int TrophySGradeId { get; set; }
    public int TrophySeqId { get; set; }
    public int EquipModelCategory { get; set; }
    public int EquipModelGender { get; set; }
    public int AccessoryCategory { get; set; }
    public int RefCategory { get; set; }
    public int SpEffectCategory { get; set; }
    public int SortGroupId { get; set; }
    public int VagrantItemLotId { get; set; }
    public int VagrantBonusEneDropItemLotId { get; set; }
    public int VagrantItemEneDropItemLotId { get; set; }
    public int IsDeposit { get; set; }
    public int IsEquipOutBrake { get; set; }
    public int DisableMultiDropShare { get; set; }
    public int IsDiscard { get; set; }
    public int IsDrop { get; set; }
    public int ShowLogCondType { get; set; }
    public int ShowDialogCondType { get; set; }
    public int Rarity { get; set; }
    public string Pad2 { get; set; } // Assuming this is a string due to the format [0|0]
    public int SaleValue { get; set; }
    public int AccessoryGroup { get; set; }
    public string Pad3 { get; set; } // Assuming this is a string due to the format [0|0]
    public int CompTrophySedId { get; set; }
    public int ResidentSpEffectId1 { get; set; }
    public int ResidentSpEffectId2 { get; set; }
    public int ResidentSpEffectId3 { get; set; }
    public int ResidentSpEffectId4 { get; set; }
    public string Pad1 { get; set; } // Assuming this is a string due to the format [0|0|0|0]
}
