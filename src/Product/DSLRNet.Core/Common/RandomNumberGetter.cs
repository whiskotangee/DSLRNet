using System.Collections.Concurrent;
namespace DSLRNet.Core.Common;

public class RandomNumberGetter
{
    readonly ConcurrentQueue<int> queue = new();
    readonly Random random;
    readonly int threshold = 5000; // Threshold to refill the queue
    readonly int maxQueueSize = 100000; // Maximum size of the queue

    public RandomNumberGetter()
        : this((int)DateTime.Now.TimeOfDay.TotalSeconds)
    {
    }

    public RandomNumberGetter(int seed)
    {
        random = new(seed);
    }

    public int NextInt(Range<int> range)
    {
        return this.NextInt(range.Min, range.Max);
    }

    public int NextInt(int minimum, int maximum)
    {
        double scale = (maximum - minimum - 1) / 100.0;
        int value = GetNextFromQueue();
        double returnValue = minimum + (value - 0) * scale;

        return (int)Math.Ceiling(returnValue);
    }
    
    public float Next(Range<float> range)
    {
        return (float)this.NextDouble(range.Min, range.Max);
    }

    public double NextDouble(double minimum = 0, double maximum = 1)
    {
        int value = GetNextFromQueue();

        return Math.Max(Math.Min(value / 100.0 * (maximum - minimum) + minimum, maximum), minimum);
    }

    public T NextWeightedValue<T>(List<T> valueList, List<int> weightList)
    {
        if (valueList.Count != weightList.Count)
        {
            //Log.Logger("Value array and Weight array have different sizes! Returning valuearray[0], " + valueList[0].ToString());
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
        return values[random.Next(values.Count)];
    }

    public List<T> GetRandomlySortedList<T>(List<T> source)
    {
        return [.. source.OrderBy(d => NextInt(0, 1000))];
    }

    public bool GetRandomBoolByPercent(double percent)
    {
        return random.NextDouble() < percent;
    }

    private int GetNextFromQueue()
    {
        if (!queue.TryDequeue(out int value) || queue.Count < threshold)
        {
            for (int i = 0; i < maxQueueSize - queue.Count; i++)
            {
                queue.Enqueue(random.Next(0, 101)); // Add random numbers between 1 and 100
            }

            if (!queue.TryDequeue(out int secondValue))
            {
                throw new Exception("Excuse me?????");
            }

            value = secondValue;
        }

        return value;
    }
}
