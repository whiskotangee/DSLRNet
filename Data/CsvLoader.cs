using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using Serilog;

namespace DSLRNet.Data;

public class CsvLoader
{
    public static List<T> LoadCsv<T>(string filename)
    {
        using var reader = new StreamReader(filename);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = (PrepareHeaderForMatchArgs args) => args.Header.ToLower(),
            MissingFieldFound = (MissingFieldFoundArgs args) => Log.Logger.Error($"{filename} missing field at index {args.Index}")
        });

        var records = csv.GetRecords<T>();
        return new List<T>(records);
    }
}
