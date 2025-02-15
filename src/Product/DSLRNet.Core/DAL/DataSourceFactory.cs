﻿namespace DSLRNet.Core.DAL;

public class DataSourceFactory(Csv csv, RegulationBinBank regulationBinReader, RandomProvider random, LocalizedNameSource nameSource, FileSourceHandler fileSourceHandler)
{
    public IDataSource<T> CreateDataSource<T>(DataSourceConfig paramSource)
        where T : ParamBase<T>, ICloneable<T>, new()
    {
        return paramSource.SourceType switch
        {
            DataSourceType.CSV => new CsvDataSource<T>(paramSource, random, csv),
            DataSourceType.RegulationBin => new RegulationBinDataSource<T>(paramSource, random, regulationBinReader, nameSource, fileSourceHandler),
            _ => throw new ArgumentException("Invalid source type"),
        };
    }
}
