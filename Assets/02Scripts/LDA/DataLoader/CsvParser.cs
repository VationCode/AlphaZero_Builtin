using System;
using System.Collections.Generic;
using System.Text;

// RFC 4180 형식의 쉼표·따옴표·행 내부 줄바꿈을 처리한다.
public static class CsvParser
{
    public static CsvTable Parse(string p_text, string p_sourceName)
    {
        if (p_text == null)
            throw new ArgumentNullException(nameof(p_text));

        string sourceName = string.IsNullOrWhiteSpace(p_sourceName)
            ? "Unknown"
            : p_sourceName.Trim();

        List<List<string>> records = ParseRecords(p_text, sourceName);

        if (records.Count == 0)
            throw new FormatException($"{sourceName}.csv가 비어 있습니다.");

        List<string> headers = CreateHeaders(records[0], sourceName);
        List<CsvRow> rows = new();

        for (int recordIndex = 1; recordIndex < records.Count; recordIndex++)
        {
            List<string> record = records[recordIndex];

            if (IsBlankRecord(record))
                continue;

            int rowNumber = recordIndex + 1;

            if (record.Count != headers.Count)
            {
                throw new FormatException(
                    $"{sourceName}.csv {rowNumber}행의 컬럼 수가 Header와 다릅니다. " +
                    $"기대값: {headers.Count}, 실제값: {record.Count}");
            }

            Dictionary<string, string> values = new(
                StringComparer.OrdinalIgnoreCase);

            for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                values.Add(headers[columnIndex], record[columnIndex]);

            rows.Add(new CsvRow(sourceName, rowNumber, values));
        }

        return new CsvTable(sourceName, headers, rows);
    }

    private static List<List<string>> ParseRecords(
        string p_text,
        string p_sourceName)
    {
        List<List<string>> records = new();
        List<string> fields = new();
        StringBuilder field = new();

        bool isQuoted = false;
        bool didCloseQuote = false;

        for (int index = 0; index < p_text.Length; index++)
        {
            char character = p_text[index];

            if (isQuoted)
            {
                if (character != '"')
                {
                    field.Append(character);
                    continue;
                }

                bool isEscapedQuote =
                    index + 1 < p_text.Length &&
                    p_text[index + 1] == '"';

                if (isEscapedQuote)
                {
                    field.Append('"');
                    index++;
                    continue;
                }

                isQuoted = false;
                didCloseQuote = true;
                continue;
            }

            if (character == '"')
            {
                if (field.Length > 0 || didCloseQuote)
                {
                    throw new FormatException(
                        $"{p_sourceName}.csv에서 잘못된 따옴표를 발견했습니다.");
                }

                isQuoted = true;
                continue;
            }

            if (character == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
                didCloseQuote = false;
                continue;
            }

            if (character == '\r' || character == '\n')
            {
                fields.Add(field.ToString());
                field.Clear();
                didCloseQuote = false;
                records.Add(fields);
                fields = new List<string>();

                if (character == '\r' &&
                    index + 1 < p_text.Length &&
                    p_text[index + 1] == '\n')
                {
                    index++;
                }

                continue;
            }

            if (didCloseQuote)
            {
                throw new FormatException(
                    $"{p_sourceName}.csv의 닫는 따옴표 뒤에 잘못된 문자가 있습니다.");
            }

            field.Append(character);
        }

        if (isQuoted)
            throw new FormatException($"{p_sourceName}.csv의 따옴표가 닫히지 않았습니다.");

        if (field.Length > 0 || fields.Count > 0 || didCloseQuote)
        {
            fields.Add(field.ToString());
            records.Add(fields);
        }

        return records;
    }

    private static List<string> CreateHeaders(
        IReadOnlyList<string> p_record,
        string p_sourceName)
    {
        List<string> headers = new(p_record.Count);
        HashSet<string> headerSet = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < p_record.Count; index++)
        {
            string header = p_record[index].Trim();

            if (index == 0)
                header = header.TrimStart('\uFEFF');

            if (string.IsNullOrEmpty(header))
            {
                throw new FormatException(
                    $"{p_sourceName}.csv Header의 {index + 1}번째 컬럼이 비어 있습니다.");
            }

            if (!headerSet.Add(header))
                throw new FormatException($"{p_sourceName}.csv Header가 중복되었습니다: {header}");

            headers.Add(header);
        }

        return headers;
    }

    private static bool IsBlankRecord(IReadOnlyList<string> p_record)
    {
        if (p_record.Count != 1)
            return false;

        return string.IsNullOrWhiteSpace(p_record[0]);
    }
}
