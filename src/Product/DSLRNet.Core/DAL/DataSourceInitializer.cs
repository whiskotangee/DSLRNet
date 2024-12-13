namespace DSLRNet.Core.DAL;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

public class DataSourceInitializer
{
    public async Task InitializeAllDataSourcesAsync(IServiceProvider serviceProvider)
    {
        var genericDataSources = serviceProvider.GetServices(typeof(IDataSource<>)).Cast<object>();
        foreach (var dataSource in genericDataSources)
        {
            var initializeMethod = dataSource.GetType().GetMethod("InitializeDataAsync"); if (initializeMethod != null)
            {
                await (Task)initializeMethod.Invoke(dataSource, null);
            }
        }
    }
}
