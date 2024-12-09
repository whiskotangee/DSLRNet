namespace DSLRNet.Core.Common;

using Org.BouncyCastle.Crypto.Modes.Gcm;

public class RandomProvider(int seed)
{
    private readonly Random random = new(seed);

    public int NextInt(IntValueRange range)
    {
        return this.NextInt(range.Min, range.Max);
    }

    public int NextInt(int minimum, int maximum)
    {
        return this.random.Next(minimum, maximum + 1);
    }

    public float Next(FloatValueRange range)
    {
        return (float)this.Next(range.Min, range.Max);
    }

    public double Next(double minimum = 0, double maximum = 1)
    {
        double value = this.random.NextDouble();

        return value * (maximum - minimum) + minimum;
    }

    public T NextWeightedValue<T>(List<WeightedValue<T>> values)
    {
        var weightTotal = values.Sum(d => d.Weight);

        int weightedResult = this.NextInt(0, weightTotal);

        foreach (var val in values)
        {
            if (weightedResult <= val.Weight)
            {
                return val.Value;
            }
            else
            {
                weightedResult -= val.Weight;
            }
        }

        return values.FirstOrDefault().Value;
    }

    public T GetRandomItem<T>(IEnumerable<T> values)
    {
        int itemNumber = this.random.Next(values.Count());

        return values.ElementAt(itemNumber);
    }

    public List<T> GetRandomItems<T>(IEnumerable<T> values, int count)
    {
        return GetRandomizedList(values).Take(count).ToList();
    }

    public List<T> GetRandomizedList<T>(IEnumerable<T> source)
    {
        return [.. source.OrderBy(d => this.NextInt(0, 1000))];
    }


    public bool PassesPercentCheck(int percent)
    {
        return this.NextInt(IntValueRange.PercentRange) < percent;
    }

    public bool PassesPercentCheck(double percent)
    {
        return this.random.NextDouble() < percent;
    }
}
