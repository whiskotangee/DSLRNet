namespace DSLRNet.Core.Data;

using System.Diagnostics;

public abstract class BaseDataSource<T> : IDataSource<T>
    where T : class, ICloneable<T>, new()
{
    private readonly RandomProvider random;

    public BaseDataSource(RandomProvider random)
    {
        this.random = random;
    }

    protected Dictionary<int, T> CachedData { get; set; }

    public T? GetItemById(int id)
    {
        if (this.CachedData.TryGetValue(id, out T item))
        {
            return item.Clone();
        }

        return null;
    }

    public T GetRandomItem()
    {
        return this.random.GetRandomItem(this.CachedData.Values.ToList()).Clone();
    }

    public IEnumerable<T> GetAll()
    {
        return this.CachedData.Values.Select(s => s.Clone()).ToList();
    }

    public abstract Task<IEnumerable<T>> LoadDataAsync();

    public async Task InitializeDataAsync()
    {
        if (this.CachedData == null)
        {
            var loadedData = await this.LoadDataAsync();
            this.CachedData = loadedData.ToDictionary(this.GetId);
            this.GetAll();
        }
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
