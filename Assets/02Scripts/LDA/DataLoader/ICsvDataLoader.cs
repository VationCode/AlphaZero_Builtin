using System.Threading.Tasks;

// CSV 원본을 Key 기반 표 데이터로 적재하는 계약을 정의한다.
public interface ICsvDataLoader
{
    Task<CsvTable> LoadAsync(string p_key);
}
