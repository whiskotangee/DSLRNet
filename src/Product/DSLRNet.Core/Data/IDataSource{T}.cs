namespace DSLRNet.Core.Data;

public interface IDataSource<T>
{
    IEnumerable<T> LoadAll();

    void ResetLoadedData();
}
