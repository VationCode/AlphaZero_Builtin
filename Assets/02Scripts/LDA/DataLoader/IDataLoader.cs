using System.Threading.Tasks;
using UnityEngine;

// 저장 위치와 관계없이 Key로 데이터를 비동기 적재하는 계약을 정의한다.
public interface IDataLoader
{
    // Key에 대응하는 원본 데이터를 요청한 형식으로 역직렬화한다.
    Task<T> LoadAsync<T>(string p_key);
}
