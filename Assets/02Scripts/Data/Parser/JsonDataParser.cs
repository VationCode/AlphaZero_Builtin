using UnityEngine;

public class JsonDataParser : IDataParser
{
    public T Parse<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }
}
