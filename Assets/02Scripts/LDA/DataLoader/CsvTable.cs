using System;
using System.Collections.Generic;
using System.Globalization;

// 하나의 CSV 파일에서 파싱된 Header와 행 목록을 보관한다.
public sealed class CsvTable
{
    private readonly HashSet<string> _headerSet;

    public string SourceName { get; }
    public IReadOnlyList<string> Headers { get; }
    public IReadOnlyList<CsvRow> Rows { get; }

    internal CsvTable(
        string p_sourceName,
        IReadOnlyList<string> p_headers,
        IReadOnlyList<CsvRow> p_rows)
    {
        SourceName = p_sourceName;
        Headers = p_headers;
        Rows = p_rows;
        _headerSet = new HashSet<string>(
            p_headers,
            StringComparer.OrdinalIgnoreCase);
    }

    // Mapper가 요구하는 스키마와 실제 Header가 정확히 일치하는지 확인한다.
    public void ValidateColumns(params string[] p_expectedColumns)
    {
        HashSet<string> expected = new(
            p_expectedColumns,
            StringComparer.OrdinalIgnoreCase);

        foreach (string column in p_expectedColumns)
        {
            if (!_headerSet.Contains(column))
            {
                throw new FormatException(
                    $"{SourceName}.csv에 필수 컬럼이 없습니다: {column}");
            }
        }

        foreach (string header in Headers)
        {
            if (!expected.Contains(header))
            {
                throw new FormatException(
                    $"{SourceName}.csv에 정의되지 않은 컬럼이 있습니다: {header}");
            }
        }
    }
}

// CSV 한 행의 원본 값과 안전한 형 변환 기능을 제공한다.
public sealed class CsvRow
{
    private readonly IReadOnlyDictionary<string, string> _values;

    public string SourceName { get; }
    public int RowNumber { get; }

    internal CsvRow(
        string p_sourceName,
        int p_rowNumber,
        IReadOnlyDictionary<string, string> p_values)
    {
        SourceName = p_sourceName;
        RowNumber = p_rowNumber;
        _values = p_values;
    }

    public string GetString(string p_column)
    {
        if (!_values.TryGetValue(p_column, out string value))
            throw CreateFormatException(p_column, "컬럼을 찾을 수 없습니다.");

        return value;
    }

    public string GetRequiredString(string p_column)
    {
        string value = GetString(p_column).Trim();

        if (string.IsNullOrEmpty(value))
            throw CreateFormatException(p_column, "필수 문자열이 비어 있습니다.");

        return value;
    }

    public int GetInt(string p_column)
    {
        string value = GetString(p_column).Trim();

        if (!int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int result))
        {
            throw CreateFormatException(
                p_column,
                $"정수로 변환할 수 없습니다: '{value}'");
        }

        return result;
    }

    public bool GetBool(string p_column)
    {
        string value = GetString(p_column).Trim();

        if (!bool.TryParse(value, out bool result))
        {
            throw CreateFormatException(
                p_column,
                $"bool로 변환할 수 없습니다: '{value}'");
        }

        return result;
    }

    public TEnum GetEnum<TEnum>(string p_column)
        where TEnum : struct, Enum
    {
        string value = GetString(p_column).Trim();

        if (!Enum.TryParse(value, true, out TEnum result) ||
            !Enum.IsDefined(typeof(TEnum), result))
        {
            throw CreateFormatException(
                p_column,
                $"{typeof(TEnum).Name} 값으로 변환할 수 없습니다: '{value}'");
        }

        return result;
    }

    public FormatException CreateFormatException(
        string p_column,
        string p_message)
    {
        return new FormatException(
            $"{SourceName}.csv {RowNumber}행 '{p_column}' 컬럼: {p_message}");
    }
}
