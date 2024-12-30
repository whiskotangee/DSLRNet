namespace DSLRNet.Core.DAL;

public class Csv(ILogger<Csv> logger)
{
    public List<T> LoadCsv<T>(string filename)
    {
        logger.LogInformation($"CSV Loading {filename}");
        using StreamReader reader = new(filename);
        using CsvReader csv = new(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = (args) => args.Header.ToLower(),
            MissingFieldFound = (args) => logger.LogError($"{filename} missing field at index {args.Index}"),
            HasHeaderRecord = true
        });

        IEnumerable<T> records = csv.GetRecords<T>();
        return new List<T>(records);
    }

    public void WriteCsv(string fileName, List<GenericParam> dictionaries)
    {
        logger.LogInformation($"CSV Writing {fileName}");

        Dictionary<string, object?>.KeyCollection headers = dictionaries.First().Properties.Keys;

        using StreamWriter writer = new(fileName);
        writer.WriteLine(string.Join(",", headers));

        foreach (GenericParam obj in dictionaries)
        {
            List<string> values = [];
            foreach (string header in headers)
            {
                if (obj.Properties.TryGetValue(header, out object? value))
                {
                    if (value is byte[] byteArray)
                    {
                        if (byteArray.Length == 1)
                        {
                            values.Add(Convert.ToInt32(byteArray[0]).ToString());
                        }
                        else
                        {
                            values.Add($"[{string.Join('|', byteArray)}]");
                        }
                    }
                    else
                    {
                        values.Add(value?.ToString() ?? string.Empty);
                    }

                }
                else
                {
                    logger.LogError($"Param file {Path.GetFileName(fileName)} had header {header} but dictionary doesn't have it?");
                }
            }

            writer.WriteLine(string.Join(",", values));
        }

        writer.Flush();
    }
}
