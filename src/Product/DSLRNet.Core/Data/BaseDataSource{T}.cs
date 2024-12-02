namespace DSLRNet.Core.Data;

public abstract class BaseDataSource<T> : IDataSource<T> 
    where T : class, ICloneable, new()
{
    private readonly RandomProvider random;

    public BaseDataSource(RandomProvider random)
    {
        this.random = random;
        ReloadData();
    }

    protected Dictionary<int, T> CachedData { get; set; }

    public T? GetItemById(int id)
    {
        if (CachedData.TryGetValue(id, out T item))
        {
            return (T)item.Clone();
        }

        return null;
    }

    public T GetRandomItem()
    {
        return (T)random.GetRandomItem(CachedData.Values.ToList()).Clone();
    }

    public IEnumerable<T> GetAll()
    {
        return CachedData.Values.Select(s => s.Clone()).Cast<T>().ToList();
    }

    public abstract IEnumerable<T> LoadData();

    public void ReloadData()
    {
        this.CachedData = LoadData().ToDictionary(GetId);
    }

    private int GetId(T value)
    {
        return (int)typeof(T).GetProperty("ID").GetValue(value);
    }

    public int Count()
    {
        return this.CachedData.Count;
    }
}
