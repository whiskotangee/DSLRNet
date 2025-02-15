﻿namespace DSLRNet.Core.DAL;

using DSLRNet.Core.Handlers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

public class RegulationBinBank
{
    private readonly Settings settings;
    private readonly ILogger<RegulationBinBank> logger;
    private readonly FileSourceHandler fileHandler;
    private readonly BND4 paramBnd;
    private readonly string sourcePath;

    private readonly ConcurrentDictionary<DataSourceNames, PARAMDEF> paramDefs = [];
    private readonly ConcurrentDictionary<DataSourceNames, List<string>> strippedNames = [];
    private readonly ConcurrentDictionary<DataSourceNames, PARAM> loadedParams = [];

    public RegulationBinBank(IOptions<Settings> settings, ILogger<RegulationBinBank> logger, FileSourceHandler fileHandler)
    {
        this.settings = settings.Value;
        this.logger = logger;
        this.fileHandler = fileHandler;
        var paramBnd = GetRegulationBin();
        this.paramBnd = paramBnd.bnd;
        this.sourcePath = paramBnd.path;
    }

    public (int updatedRows, int addedRows) AddOrUpdateRows(DataSourceNames dataSourceName, IEnumerable<ParamEdit> paramEdits)
    {
        int updatedRows = 0;
        int addedRows = 0;

        PARAM param = loadedParams[dataSourceName];

        foreach(ParamEdit? edit in paramEdits.OrderBy(d => d.ParamObject.ID))
        {
            PARAM.Row? row = param.Rows.SingleOrDefault(d => d.ID == edit.ParamObject.ID);
            updatedRows += row != null ? 1 : 0;
            addedRows += row == null ? 1 : 0;

            if (row == null)
            {
                row = new(edit.ParamObject.ID, edit.ParamObject.Name, param.AppliedParamdef);
                param.Rows.Add(row);
            }

            foreach (string fieldName in edit.ParamObject.Properties.Keys.Where(d => d != "ID" && d != "Name"))
            {
                PARAM.Cell cell = row.Cells.Single(d => d.Def.InternalName == fieldName);
                cell.Value = edit.ParamObject.Properties[cell.Def.InternalName];
            }
        }

        UpdateParam(dataSourceName, param);

        return (updatedRows, addedRows);
    }

    public void SaveRegulationBin(string path)
    {
        SFUtil.EncryptERRegulation(path, paramBnd);
    }

    public void UpdateParam(DataSourceNames name, PARAM param)
    {
        param.Rows = [.. param.Rows.OrderBy(d => d.ID)];

        foreach (BinderFile f in this.paramBnd.Files)
        {
            string paramName = Path.GetFileNameWithoutExtension(f.Name);

            if (Enum.TryParse(paramName, out DataSourceNames readName) && readName == name)
            {
                f.Bytes = param.Write();
                break;
            }
        }
    }

    public PARAM GetParam(DataSourceNames paramName)
    {
        return loadedParams.GetOrAdd(paramName, (name) =>
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                foreach (BinderFile f in this.paramBnd.Files)
                {
                    string paramNameStr = Path.GetFileNameWithoutExtension(f.Name);

                    if (Enum.TryParse(paramNameStr, out DataSourceNames readName) && readName == name)
                    {
                        this.logger.LogInformation($"Creating PARAM object for {name}");
                        PARAM readParam = PARAM.Read(f.Bytes);
                        readParam.ApplyParamdef(GetParamDef(name));
                        this.ApplyStrippedNames(readParam, paramNameStr);
                        return readParam;
                    }
                }
            } 
            finally
            {
                this.logger.LogInformation($"Getting param {paramName} took {stopwatch.ElapsedMilliseconds}ms");
            }

            throw new ArgumentException($"Param {paramName} was not found");
        });
    }

    private void ApplyStrippedNames(PARAM param, string name)
    {
        var modDefinitionNames = Directory.EnumerateFiles(Path.GetDirectoryName(this.sourcePath), $"{name}.txt", SearchOption.AllDirectories);

        foreach (string modDefinitionName in modDefinitionNames)
        {
            string[] strippedNames = File.ReadAllLines(modDefinitionName);
            for (int i = 0;i < strippedNames.Length; i++)
            {
                var strippedName = strippedNames[i].Split('-')[0];
                // set the name to the strippedName up until the first non letter or space character
                string? trimmedName = new string(strippedName.Where(d => Char.IsLetter(d) || d == ' ').ToArray());
                param.Rows[i].Name = trimmedName ?? param.Rows[i].Name;
            }
        }
    }

    private PARAMDEF GetParamDef(DataSourceNames paramName)
    {
        return paramDefs.GetOrAdd(paramName, (name) =>
        {
            string fileName = GetFileName(name);

            string path = PathHelper.FullyQualifyAppDomainPath($"Assets\\Data\\PARAM\\ER\\Defs\\{fileName}.xml");
            this.logger.LogInformation($"Loading PARAMDEF {path} for {fileName}");
            return PARAMDEF.XmlDeserialize(path);
        });
    }

    private string GetFileName(DataSourceNames paramName)
    {
        string fileName = paramName.ToString();

        switch (paramName)
        {
            case DataSourceNames.ItemLotBase:
            case DataSourceNames.ItemLotParam_enemy:
            case DataSourceNames.ItemLotParam_map:
                fileName = "ItemLotParam";
                break;
            case DataSourceNames.SpEffectParam:
                fileName = "SpEffect";
                break;
        }

        return fileName;
    }

    private (BND4 bnd, string path) GetRegulationBin()
    {
        if (!fileHandler.TryGetFile("regulation.pre-dslr.bin", out string regBinPath))
        {
            if (!fileHandler.TryGetFile("regulation.bin", out regBinPath))
            {
                regBinPath = Path.Combine(this.settings.GamePath, "regulation.bin");
            }
        }

        this.logger.LogInformation($"Loading regulation bin from {regBinPath}");

        return (SFUtil.DecryptERRegulation(regBinPath), regBinPath);
    }
}
