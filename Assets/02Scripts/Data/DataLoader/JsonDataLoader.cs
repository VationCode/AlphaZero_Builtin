using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// 데이터의 출처(Source)로부터 데이터를 가져오는 것
public class JsonDataLoader : IDataLoader
{
    private readonly string _rootPath;

    public JsonDataLoader(string p_rootPath)
    {
        _rootPath = p_rootPath;
    }

    public async Task<T> LoadAsync<T>(string p_key)
    {
        // LoadAsync<WeaponTableDTO>("Weapon"); 이런식으로 편하게 사용 가능하게 하려고
        // _rootPath는 현재 리소스폴더 Data폴더 경로로
        string path = Path.Combine(_rootPath, $"{p_key}.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Json file not found : {path}");
        }

        // JSON 파일 읽기
        string json = await File.ReadAllTextAsync(path);

        //JsonUtility 파싱
        return JsonUtility.FromJson<T>(json);
    }
}