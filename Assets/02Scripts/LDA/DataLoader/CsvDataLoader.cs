using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// Resources 아래의 CSV TextAsset을 읽어 표 데이터로 변환한다.
public sealed class CsvDataLoader : ICsvDataLoader
{
    private readonly string _resourceRoot;

    public CsvDataLoader(string p_resourceRoot)
    {
        _resourceRoot = NormalizeResourcePath(p_resourceRoot);
    }

    // 호출자는 확장자를 제외한 논리 Key를 전달한다.
    public Task<CsvTable> LoadAsync(string p_key)
    {
        if (string.IsNullOrWhiteSpace(p_key))
            throw new ArgumentException("CSV Key가 비어 있습니다.", nameof(p_key));

        string key = p_key.Trim();
        string resourcePath = string.IsNullOrEmpty(_resourceRoot)
            ? key
            : $"{_resourceRoot}/{key}";

        TextAsset csvAsset = Resources.Load<TextAsset>(resourcePath);

        if (csvAsset == null)
        {
            throw new FileNotFoundException(
                $"CSV TextAsset을 찾을 수 없습니다: Resources/{resourcePath}.csv");
        }

        CsvTable table = CsvParser.Parse(csvAsset.text, key);
        return Task.FromResult(table);
    }

    private static string NormalizeResourcePath(string p_path)
    {
        return string.IsNullOrWhiteSpace(p_path)
            ? string.Empty
            : p_path.Trim().Replace('\\', '/').Trim('/');
    }
}
