namespace DSLRNet.Core.Common;

using DSLRNet.Core.Extensions;

public class PathHelper
{
    public static string FullyQualifyAppDomainPath(params string[] pathParts)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(pathParts));
    }
}
