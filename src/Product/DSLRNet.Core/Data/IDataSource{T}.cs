namespace DSLRNet.Core.Data;

public interface IDataSource<T>
{
    int Count();

    IEnumerable<T> GetAll();

    T GetRandomItem();

    T GetItemById(int id);

    void ReloadData();
}
