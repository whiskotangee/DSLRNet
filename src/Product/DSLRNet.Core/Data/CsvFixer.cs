namespace DSLRNet.Core.Data;

public class CsvFixer
{
    public class Entry
    {
        public string Text { get; set; }
        public List<int> IDList { get; set; }
    }

    public class JsonData
    {
        public int FMG_ID { get; set; }
        public List<Entry> Entries { get; set; }
    }

    public class CsvRecord
    {
        public int ID { get; set; }
        public string Name { get; set; }
        // Other fields can be added here
        public string OtherField1 { get; set; }
        public string OtherField2 { get; set; }
        // Add as many fields as needed
    }

    public static void GenerateClassFromCsv(string csvFilePath)
    {
        string[] lines = File.ReadAllLines(csvFilePath);
        if (lines.Length < 2)
        {
            throw new InvalidOperationException("CSV file must contain at least two lines (header and one data row).");
        }

        string[] headers = lines[0].Split(',', StringSplitOptions.RemoveEmptyEntries);
        string[][] dataRows = lines.Skip(1).Select(line => line.Split(',')).ToArray();

        List<string> properties = [];

        for (int i = 0; i < headers.Length; i++)
        {
            string header = headers[i];
            IEnumerable<string> columnValues = dataRows.Select(row => row[i]);
            string type = DetermineType(columnValues);
            properties.Add($"public {type} {header} {{ get; set; }}");
        }

        string classDefinition = $@"
namespace DSLRNet.Core.Contracts.Params;

public partial class {Path.GetFileNameWithoutExtension(csvFilePath)} : ParamBase
{{
    {string.Join(Environment.NewLine + "    ", properties)}
}}";

        string path = $"{Path.Combine("O:\\EldenRingShitpostEdition\\Tools\\DSLRNet\\Data\\Generated", Path.GetFileNameWithoutExtension(csvFilePath))}.cs";

        File.WriteAllText(path, classDefinition);

        Console.WriteLine(classDefinition);
    }

    private static void UpdateCsvNames<T>(string csvFilePath, List<string> jsonFilePaths)
    {
        foreach (var jsonFilePath in jsonFilePaths)
        {
            // Read JSON file
            JsonData? jsonData = JsonConvert.DeserializeObject<JsonData>(File.ReadAllText(jsonFilePath));

            // Read CSV file
            CsvConfiguration csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };

            List<T> csvRecords;
            using (StreamReader reader = new StreamReader(csvFilePath))
            using (CsvReader csv = new CsvReader(reader, csvConfig))
            {
                csvRecords = csv.GetRecords<T>().ToList();
            }

            // Update CSV records with JSON data
            foreach (Entry entry in jsonData.Entries)
            {
                foreach (int id in entry.IDList)
                {
                    T? record = csvRecords.FirstOrDefault(r => (int)r.GetType().GetProperty("ID").GetValue(r) == id);
                    if (record != null)
                    {
                        record.GetType().GetProperty("Name").SetValue(record, entry.Text);
                    }
                }
            }

            // Write updated records back to CSV file
            using (StreamWriter writer = new StreamWriter(csvFilePath))
            using (CsvWriter csv = new CsvWriter(writer, csvConfig))
            {
                csv.WriteRecords(csvRecords);
            }

            File.Copy(csvFilePath, Path.Combine("O:\\EldenRingShitpostEdition\\Tools\\DSLRNet\\DefaultData\\ER\\CSVs\\", Path.GetFileName(csvFilePath)), true);
        }
        Console.WriteLine("CSV file updated successfully.");
    }

    public static void UpdateNamesInCSVs()
    {
        UpdateCsvNames<EquipParamWeapon>("DefaultData\\ER\\CSVs\\EquipParamWeapon.csv", Directory.GetFiles("DefaultData\\ER\\FMGBase\\", "TitleWeapons*.fmgmerge.json").ToList());
        UpdateCsvNames<EquipParamProtector>("DefaultData\\ER\\CSVs\\EquipParamProtector.csv", Directory.GetFiles("DefaultData\\ER\\FMGBase\\", "TitleArmor*.fmgmerge.json").ToList());
        UpdateCsvNames<EquipParamAccessory>("DefaultData\\ER\\CSVs\\EquipParamAccessory.csv", Directory.GetFiles("DefaultData\\ER\\FMGBase\\", "TitleRings*.fmgmerge.json").ToList());
    }

    private static string DetermineType(IEnumerable<string> values)
    {
        if (values.All(d => int.TryParse(d, out _)))
        {
            return "int";
        }

        if (values.All(d => float.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
        {
            return "float";
        }

        return "string";
    }

    internal static void AddNewHeaders(string csvFile)
    {
        string latestFile = Path.Combine("DefaultData\\ER\\CSVs\\LatestParams", Path.GetFileName(csvFile));

        if (!File.Exists(latestFile))
        {
            return;
        }

        CsvConfiguration csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using StreamReader reader = new StreamReader(csvFile);
        using CsvReader oldCsv = new CsvReader(reader, csvConfig);
        using StreamReader newReader = new StreamReader(latestFile);
        using CsvReader newCsv = new CsvReader(newReader, csvConfig);

        oldCsv.Read();
        oldCsv.ReadHeader();
        newCsv.Read();
        newCsv.ReadHeader();

        List<string> oldHeaders = oldCsv.HeaderRecord.ToList();
        string[]? newHeaders = newCsv.HeaderRecord;

        newCsv.Read();
        dynamic firstNewRecord = newCsv.GetRecord<dynamic>();
        Dictionary<string, object> firstNewRecordDict = ((IDictionary<string, object>)firstNewRecord).ToDictionary(k => k.Key, v => v.Value);

        List<string> missingHeaders = newHeaders.Where(d => !oldHeaders.Any(header => header.Equals(d, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(d)).ToList();

        if (missingHeaders.Count != 0)
        {
            foreach (string? missingHeader in missingHeaders)
            {
                Log.Logger.Warning($"Adding header {missingHeader} that was missing from csv {oldCsv}");
                oldHeaders.Add(missingHeader);
            }

            List<Dictionary<string, object>> records = new List<Dictionary<string, object>>();

            while (oldCsv.Read())
            {
                dynamic record = oldCsv.GetRecord<dynamic>();
                Dictionary<string, object> recordDict = ((IDictionary<string, object>)record).ToDictionary(k => k.Key, v => v.Value);
                records.Add(recordDict);
            }

            reader.Dispose();

            // Write the updated headers and records back to the file
            using StreamWriter writer = new StreamWriter(csvFile);
            using CsvWriter csvWriter = new CsvWriter(writer, csvConfig);

            csvWriter.WriteField(newHeaders);
            csvWriter.NextRecord();

            foreach (Dictionary<string, object> record in records)
            {
                foreach (string header in newHeaders)
                {
                    csvWriter.WriteField(record.TryGetValue(header, out object? value) ? value : ((IDictionary<string, object>)firstNewRecord)[header]);
                }
                csvWriter.NextRecord();
            }

            csvWriter.Flush();
            writer.Flush();
        }
    }
}
