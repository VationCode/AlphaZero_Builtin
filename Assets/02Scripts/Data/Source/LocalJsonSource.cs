using System.IO;
using System.Threading.Tasks;

// 데이터의 출처(Source)로부터 데이터를 가져오는 것
public class LocalJsonSource : IDataSource
{
    private readonly string _rootPath;

    public LocalJsonSource(string p_rootPath)
    {
        _rootPath = p_rootPath;
    }

    public async Task<string> GetDataAsync(string p_key)
    {
        string path = Path.Combine(_rootPath, $"{p_key}.json");
        return await File.ReadAllTextAsync(path);
    }
}