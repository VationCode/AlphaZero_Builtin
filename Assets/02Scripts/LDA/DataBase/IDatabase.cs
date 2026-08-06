using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// IDatabase 구현체가 제공해야 하는 기능 계약을 정의한다.
public interface IDatabase<TKey, TValue>
{
    // 초기 데이터 한 건을 등록한다.
    void Add(TKey p_key, TValue p_value);   // Initialize에서만 사용

    // Key가 있을 때 데이터와 성공 여부를 반환한다.
    bool TryGet(TKey p_key, out TValue p_value);

    // Key에 대응하는 데이터를 반환한다.
    TValue Get(TKey p_key);

    // Key의 등록 여부를 확인한다.
    bool Contains(TKey p_key);

    // 등록된 전체 데이터를 읽기 전용으로 노출한다.
    IReadOnlyDictionary<TKey, TValue> GetAll();

    // 등록된 데이터를 모두 제거한다.
    void Clear();
}
