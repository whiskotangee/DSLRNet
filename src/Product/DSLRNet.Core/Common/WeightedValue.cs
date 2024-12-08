namespace DSLRNet.Core.Common;

public class WeightedValue<T>
{
    public T Value { get; set; }

    public int Weight { get; set; }

    public static List<WeightedValue<T>> CreateFromLists(List<T> valuesList, List<int> weightList)
    {
        if (valuesList.Count != weightList.Count)
        {
            throw new ArgumentException("The values list and weight list must have the same number of elements.");
        }

        List<WeightedValue<T>> weightedValues = [];

        for (int i = 0; i < valuesList.Count; i++)
        {
            weightedValues.Add(new WeightedValue<T>
            {
                Value = valuesList[i],
                Weight = weightList[i]
            });
        }

        return weightedValues;
    }
}

