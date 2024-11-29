namespace DSLRNet.Core.Data;

public class Csv
{
    public static List<T> LoadCsv<T>(string filename)
    {
        Log.Logger.Information($"CSV Loading {filename}");
        using StreamReader reader = new StreamReader(filename);
        using CsvReader csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = (PrepareHeaderForMatchArgs args) => args.Header.ToLower(),
            MissingFieldFound = (MissingFieldFoundArgs args) => Log.Logger.Error($"{filename} missing field at index {args.Index}"),
            HasHeaderRecord = true
        });

        IEnumerable<T> records = csv.GetRecords<T>();
        return new List<T>(records);
    }

    public static void WriteCsv(string fileName, List<GenericDictionary> dictionaries)
    {
        Log.Logger.Information($"CSV Writing {fileName}");

        Dictionary<string, object?>.KeyCollection headers = dictionaries.First().Properties.Keys;

        using StreamWriter writer = new StreamWriter(fileName);
        writer.WriteLine(string.Join(",", headers));

        foreach (GenericDictionary obj in dictionaries)
        {
            List<string> values = [];
            foreach (string header in headers)
            {
                if (obj.Properties.TryGetValue(header, out object? value))
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
