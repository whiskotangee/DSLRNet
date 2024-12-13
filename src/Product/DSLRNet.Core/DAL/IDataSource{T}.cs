namespace DSLRNet.Core.Data;

public interface IDataSource
{
    Task InitializeDataAsync();
}

public interface IDataSource<T> : IDataSource
{
    IEnumerable<T> GetAll();

    T GetRandomItem();

    T GetItemById(int id);

    int Count();
}
