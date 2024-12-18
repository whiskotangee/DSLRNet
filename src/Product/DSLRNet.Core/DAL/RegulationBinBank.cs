namespace DSLRNet.Core.DAL;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

public class RegulationBinBank
{
    private readonly Configuration configuration;
    private readonly ILogger<RegulationBinBank> logger;
    private readonly BND4 paramBnd;

    private readonly ConcurrentDictionary<DataSourceNames, PARAMDEF> paramDefs = [];
    private readonly ConcurrentDictionary<DataSourceNames, PARAM> loadedParams = [];

    public RegulationBinBank(IOptions<Configuration> configuration, ILogger<RegulationBinBank> logger)
    {
        this.configuration = configuration.Value;
        this.logger = logger;

        var regBinPath = Path.Combine(this.configuration.Settings.DeployPath, "regulation.pre-dslr.bin");
        if (!File.Exists(regBinPath))
        {
            regBinPath = Path.Combine(this.configuration.Settings.DeployPath, "regulation.bin");
            if (!File.Exists(regBinPath))
            {
                regBinPath = Path.Combine(this.configuration.Settings.GamePath, "regulation.predslr.bin");
            }
        }

        this.logger.LogInformation($"Loading regulation bin from {regBinPath}");
        paramBnd = GetRegulationBin();
    }

    public (int updatedRows, int addedRows) AddOrUpdateRows(DataSourceNames dataSourceName, List<ParamEdit> paramEdits)
    {
        int updatedRows = 0;
        int addedRows = 0;

        PARAM param = loadedParams[dataSourceName];

        foreach(var edit in paramEdits.OrderBy(d => d.ParamObject.ID))
        {
            var row = param.Rows.SingleOrDefault(d => d.ID == edit.ParamObject.ID);
            updatedRows += row != null ? 1 : 0;
            addedRows += row == null ? 1 : 0;

            if (row == null)
            {
                row = new(edit.ParamObject.ID, string.Empty, param.AppliedParamdef);
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
        foreach (BinderFile f in this.paramBnd.Files)
        {
            var paramName = Path.GetFileNameWithoutExtension(f.Name);

            if (Enum.TryParse<DataSourceNames>(paramName, out var readName) && readName == name)
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
                    var paramName = Path.GetFileNameWithoutExtension(f.Name);

                    if (Enum.TryParse<DataSourceNames>(paramName, out var readName) && readName == name)
                    {
                        this.logger.LogInformation($"Creating PARAM object for {name}");
                        PARAM readParam = PARAM.Read(f.Bytes);
                        readParam.ApplyParamdef(GetParamDef(name));
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

    private PARAMDEF GetParamDef(DataSourceNames paramName)
    {
        return paramDefs.GetOrAdd(paramName, (name) =>
        {
            string fileName = GetFileName(name);

            var path = $"Assets\\Data\\PARAM\\ER\\Defs\\{fileName}.xml";
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
    private BND4 GetRegulationBin()
    {
        string regulationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.pre-dslr.bin");

        if (!File.Exists(regulationFile))
        {
            regulationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.bin");
            if (!File.Exists(regulationFile))
            {
                regulationFile = Path.Combine(this.configuration.Settings.GamePath, "regulation.bin");
            }
        }

        return SFUtil.DecryptERRegulation(regulationFile);
    }
}
