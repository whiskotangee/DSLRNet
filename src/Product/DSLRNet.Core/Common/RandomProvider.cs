namespace DSLRNet.Core.Common;

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

        return values.First().Value;
    }

    public T GetRandomItem<T>(IEnumerable<T> values)
    {
        int itemNumber = this.NextInt(0, values.Count() - 1);

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
        if (percent < 100)
        {
            return this.NextInt(IntValueRange.PercentRange) < percent;
        }
        else
        {
            return true;
        }
    }

    public bool PassesPercentCheck(double percent)
    {
        if (percent < 1.0)
        {
            return this.Next() < percent;
        }
        else
        {
            return true;
        }
    }
}