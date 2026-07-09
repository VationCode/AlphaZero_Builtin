using System.Threading.Tasks;
using UnityEngine;

// 보통 로더는 비동기 처리로 데이터를 불러오기 때문에 Task를 반환하는 비동기 메서드를 정의
public interface IDataSource
{
    Task<string> GetDataAsync(string p_key);
}