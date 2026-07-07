using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseDatabase<TKey, TValue> : IDatabase<TKey, TValue>
{

    protected readonly Dictionary<TKey, TValue> _dataDict = new();

    public virtual void Add(TKey p_key, TValue p_value)
    {
        _dataDict[p_key] = p_value;
    }

    public virtual TValue Get(TKey p_key)
    {
        return _dataDict[p_key];
    }

    public virtual bool TryGet(TKey p_key, out TValue p_value)
    {
        return _dataDict.TryGetValue(p_key, out p_value);
    }

    public virtual bool Contains(TKey p_key)
    {
        return _dataDict.ContainsKey(p_key);
    }

    public virtual IReadOnlyDictionary<TKey, TValue> GetAll()
    {
        return _dataDict;
    }

    public virtual void Clear()
    {
        _dataDict.Clear();
    }
}
