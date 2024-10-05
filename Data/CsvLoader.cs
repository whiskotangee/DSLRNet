using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using Serilog;

namespace DSLRNet.Data;

public class CsvLoader
{
    public static List<T> LoadCsv<T>(string filename)
    {
        using StreamReader reader = new StreamReader(filename);
        using CsvReader csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = (PrepareHeaderForMatchArgs args) => args.Header.ToLower(),
            MissingFieldFound = (MissingFieldFoundArgs args) => Log.Logger.Error($"{filename} missing field at index {args.Index}")
        });

        IEnumerable<T> records = csv.GetRecords<T>();
        return new List<T>(records);
    }
}
