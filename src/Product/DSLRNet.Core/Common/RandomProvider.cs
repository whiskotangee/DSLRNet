namespace DSLRNet.Core.Common;

public class RandomProvider(int seed)
{
    private readonly Random random = new(seed);

    public int NextInt(Range<int> range)
    {
        return this.NextInt(range.Min, range.Max);
    }

    public int NextInt(int minimum, int maximum)
    {
        return this.random.Next(minimum, maximum + 1);
    }

    public float Next(Range<float> range)
    {
        return (float)this.NextDouble(range.Min, range.Max);
    }

    public double NextDouble(double minimum = 0, double maximum = 1)
    {
        double value = this.random.NextDouble();

        return value * (maximum - minimum) + minimum;
    }

    public T NextWeightedValue<T>(List<T> valueList, List<int> weightList)
    {
        if (valueList.Count != weightList.Count)
        {
            return valueList[0];
        }
        else
        {
            int weightTotal = 0;
            List<int> weightTotalStepsArray = [];
            for (int x = 0; x < weightList.Count; x++)
            {
                weightTotal += weightList[x];
                weightTotalStepsArray.Add(weightTotal);
            }

            int weightedResult = this.NextInt(0, weightTotal);

            for (int x = 0; x < weightTotalStepsArray.Count; x++)
            {
                if (weightedResult <= weightTotalStepsArray[x])
                {
                    return valueList[x];
                }
            }
        }

        throw new Exception("Failure");
    }

    public T GetRandomItem<T>(List<T> values)
    {
        return values[this.random.Next(values.Count)];
    }

    public List<T> GetRandomizedList<T>(IEnumerable<T> source)
    {
        return [.. source.OrderBy(d => this.NextInt(0, 1000))];
    }

    public bool GetBoolByPercent(int percent)
    {
        return this.NextInt(Range<int>.PercentRange) < percent;
    }

    public bool GetRandomBoolByPercent(double percent)
    {
        return this.random.NextDouble() < percent;
    }
}
