﻿namespace DSLRNet.Core.DAL;
public interface IDataSource
{
    Task InitializeDataAsync(IEnumerable<int>? ignoreIds = null);
}

public interface IDataSource<T> : IDataSource
    where T : class, ICloneable<T>
{
    IEnumerable<T> GetAll();

    T GetRandomItem();

    T? GetItemById(int id);

    int Count();
}
