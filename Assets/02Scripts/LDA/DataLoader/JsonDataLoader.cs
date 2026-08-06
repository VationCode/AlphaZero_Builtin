using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// 지정된 Root 경로의 JSON 파일을 읽어 요청한 데이터 형식으로 역직렬화한다.
public class JsonDataLoader : IDataLoader
{
    private readonly string _rootPath;

    // JSON 파일을 찾을 기준 Root 경로를 보관한다.
    public JsonDataLoader(string p_rootPath)
    {
        _rootPath = p_rootPath;
    }

    // Key.json 파일을 비동기로 읽고 Unity JsonUtility로 역직렬화한다.
    public async Task<T> LoadAsync<T>(string p_key)
    {
        // 호출자는 확장자를 제외한 논리 Key만 전달한다.
        string path = Path.Combine(_rootPath, $"{p_key}.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Json file not found : {path}");
        }

        // 파일 입출력은 Main Thread를 막지 않도록 비동기로 수행한다.
        string json = await File.ReadAllTextAsync(path);

        // JSON 문자열을 호출자가 요청한 Wrapper 또는 DTO 형식으로 변환한다.
        return JsonUtility.FromJson<T>(json);
    }
}
