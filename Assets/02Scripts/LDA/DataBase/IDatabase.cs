using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IDatabase<TKey, TValue>
{
    void Add(TKey p_key, TValue p_value);   // Initialize에서만 사용

    bool TryGet(TKey p_key, out TValue p_value);

    TValue Get(TKey p_key);

    bool Contains(TKey p_key);
    IReadOnlyDictionary<TKey, TValue> GetAll();

    void Clear();
}
