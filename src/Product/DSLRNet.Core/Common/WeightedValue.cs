namespace DSLRNet.Core.Common;

public class WeightedValue<T>
{
    public T Value { get; set; }

    public int Weight { get; set; }

    public static List<WeightedValue<T>> CreateFromLists(List<T> valuesList, List<int> weightList)
    {
        // If the values list is shorter, add random elements from itself
        while (valuesList.Count < weightList.Count)
        {
            valuesList.Add(valuesList.Last());
        }

        // If the weight list is shorter, add random elements from itself
        while (weightList.Count < valuesList.Count)
        {
            weightList.Add(weightList.LastOrDefault(10));
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

