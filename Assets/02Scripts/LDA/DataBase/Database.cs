using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

// 늘어날 Database를 위해서 인터페이스를 만들어서 공통적인 기능을 정의한다.
// Skill, Item, Quest 등등의 Database를 만들 때, 이 인터페이스를 상속받아서 구현하면 된다.
public abstract class Database<TKey, TValue> : IDatabase<TKey, TValue>
{
    protected readonly IDataLoader _Loader;

    protected readonly Dictionary<TKey, TValue> _Dict = new();

    protected Database(IDataLoader p_loader)
    {
        _Loader = p_loader;
    }

    public void Add(TKey p_key, TValue p_value)
    {
        _Dict.Add(p_key, p_value);
    }

    public TValue Get(TKey p_key)
    {
        return _Dict[p_key];
    }

    public bool TryGet(TKey p_key, out TValue p_value)
    {
        return _Dict.TryGetValue(p_key, out p_value);
    }

    public bool Contains(TKey p_key)
    {
        return _Dict.ContainsKey(p_key);
    }

    public IReadOnlyDictionary<TKey, TValue> GetAll()
    {
        return _Dict;
    }

    public void Clear()
    {
        _Dict.Clear();
    }
}
