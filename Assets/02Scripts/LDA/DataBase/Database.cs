using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

// Loader와 Key 기반 Dictionary를 공유하는 Database의 공통 등록·조회 기능을 제공한다.
public abstract class Database<TKey, TValue> : IDatabase<TKey, TValue>
{
    protected readonly IDataLoader _Loader;

    protected readonly Dictionary<TKey, TValue> _Dict = new();

    // 파생 Database가 사용할 데이터 Loader를 보관한다.
    protected Database(IDataLoader p_loader)
    {
        _Loader = p_loader;
    }

    // 초기화 중 Key와 데이터를 Dictionary에 등록한다.
    public void Add(TKey p_key, TValue p_value)
    {
        _Dict.Add(p_key, p_value);
    }

    // Key에 대응하는 데이터를 반환하며, 없는 Key는 Dictionary 예외로 알린다.
    public TValue Get(TKey p_key)
    {
        return _Dict[p_key];
    }

    // TryGet 조건을 검사하고 성공 여부와 결과를 반환한다.
    public bool TryGet(TKey p_key, out TValue p_value)
    {
        return _Dict.TryGetValue(p_key, out p_value);
    }

    // 주어진 Key의 등록 여부를 확인한다.
    public bool Contains(TKey p_key)
    {
        return _Dict.ContainsKey(p_key);
    }

    // 외부에서 수정할 수 없는 전체 데이터 View를 반환한다.
    public IReadOnlyDictionary<TKey, TValue> GetAll()
    {
        return _Dict;
    }

    // Clear 상태를 초기값으로 비운다.
    public void Clear()
    {
        _Dict.Clear();
    }
}
