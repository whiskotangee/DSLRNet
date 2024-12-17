namespace DSLRNet.Core.Common;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

public class RandomProvider(int seed)
{
    private readonly Random random = new(seed);

    public int NextInt(IntValueRange range, [CallerMemberName] string caller = "Unknown")
    {
        return this.NextInt(range.Min, range.Max, caller);
    }

    public int NextInt(int minimum, int maximum, [CallerMemberName] string caller = "Unknown")
    {
        return this.random.Next(minimum, maximum + 1);
    }

    public float Next(FloatValueRange range, [CallerMemberName] string caller = "Unknown")
    {
        return (float)this.Next(range.Min, range.Max, caller);
    }

    public double Next(double minimum = 0, double maximum = 1, [CallerMemberName] string caller = "Unknown")
    {
        double value = this.random.NextDouble();

        return value * (maximum - minimum) + minimum;
    }

    public T NextWeightedValue<T>(List<WeightedValue<T>> values, [CallerMemberName] string caller = "Unknown")
    {
        var weightTotal = values.Sum(d => d.Weight);

        int weightedResult = this.NextInt(0, weightTotal, caller);

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

    public T GetRandomItem<T>(IEnumerable<T> values, [CallerMemberName] string caller = "Unknown")
    {
        int itemNumber = this.NextInt(0, values.Count() - 1, caller);

        return values.ElementAt(itemNumber);
    }

    public List<T> GetRandomItems<T>(IEnumerable<T> values, int count, [CallerMemberName] string caller = "Unknown")
    {
        return GetRandomizedList(values, caller).Take(count).ToList();
    }

    public List<T> GetRandomizedList<T>(IEnumerable<T> source, [CallerMemberName] string caller = "Unknown")
    {
        return [.. source.OrderBy(d => this.NextInt(0, 1000, caller))];
    }

    public bool PassesPercentCheck(int percent, [CallerMemberName] string caller = "Unknown")
    {
        if (percent < 100)
        {
            return this.NextInt(IntValueRange.PercentRange, caller) < percent;
        }
        else
        {
            return true;
        }
    }

    public bool PassesPercentCheck(double percent, [CallerMemberName] string caller = "Unknown")
    {
        if (percent < 100)
        {
            return this.Next(0, percent, caller) < percent;
        }
        else
        {
            return true;
        }
    }
}