namespace DSLRNet.Core.Data;
public abstract class BaseDataSource<T>(RandomProvider random) : IDataSource<T>
    where T : class, ICloneable<T>, new()
{
    protected Dictionary<int, T> CachedData { get; set; } = [];

    public T? GetItemById(int id)
    {
        if (this.CachedData.TryGetValue(id, out T? item))
        {
            return item.Clone();
        }

        return null;
    }

    public T GetRandomItem()
    {
        return random.GetRandomItem(this.CachedData.Values.ToList()).Clone();
    }

    public IEnumerable<T> GetAll()
    {
        return this.CachedData.Values.ToList();
    }

    public abstract Task<IEnumerable<T>> LoadDataAsync();

    public async Task InitializeDataAsync(IEnumerable<int>? ignoreIds = null)
    {
        if (this.CachedData.Count == 0)
        {
            var loadedData = await this.LoadDataAsync();
            this.CachedData = loadedData.Where(d => ignoreIds == null || !ignoreIds.Contains(this.GetId(d))).ToDictionary(this.GetId);
        }
    }

    private int GetId(T value)
    {
        return (int)(typeof(T).GetProperty("ID")?.GetValue(value) ?? -1);
    }

    public int Count()
    {
        return this.CachedData.Count;
    }
}
