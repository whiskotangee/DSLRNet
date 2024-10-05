using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using Serilog;

namespace DSLRNet.Data;

public class Csv
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

    public static void WriteCsv(string fileName, List<GenericDictionary> dictionaries)
    {
        var headers = dictionaries.First().Properties.Keys;

        using var writer = new StreamWriter(fileName);
        writer.WriteLine(string.Join(",", headers));
        
        foreach(var obj in dictionaries)
        {
            List<string> values = [];
            foreach (var header in headers)
            {
                if (obj.Properties.TryGetValue(header, out var value))
                {
                    values.Add(value?.ToString());
                }
                else
                {
                    Log.Logger.Error($"Param file {Path.GetFileName(fileName)} had header {header} but dictionary doesn't have it?");
                }
            }

            writer.WriteLine(string.Join(",", values));
        }

        writer.Flush();
    }
}
