namespace DSLRNet.Core.Config;

using IniParser.Model;
using System;

public class ArmorGeneratorSettings
{ 
    public string CutRateDescriptionTemplate { get; set; } = string.Empty;
    public int ResistParamBuffCount { get; set; }
    public int CutRateParamBuffCount { get; set; }

    public void Initialize(IniData data)
    {
        var section = "Settings.ArmorGeneratorSettings";
        if (data.Sections.ContainsSection(section))
        {
            var armorSection = data[section];
            CutRateDescriptionTemplate = armorSection.ContainsKey("CutRateDescriptionTemplate") ? armorSection["CutRateDescriptionTemplate"] : string.Empty;
            ResistParamBuffCount = armorSection.ContainsKey("ResistParamBuffCount") && int.TryParse(armorSection["ResistParamBuffCount"], out int val) ? val : 3;
            CutRateParamBuffCount = armorSection.ContainsKey("CutRateParamBuffCount") && int.TryParse(armorSection["CutRateParamBuffCount"], out val) ? val : 3;
        }
    }
}
