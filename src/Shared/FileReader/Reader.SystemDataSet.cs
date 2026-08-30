using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;

namespace Shared.FileReader
{
    public static partial class Reader
    {
        public class SystemDataSet : IFileReader
        {
            private readonly WorksheetProcessor _processor;
            private readonly IFileLoader<DataSet> _loader;

            public SystemDataSet(WorksheetProcessor processor, IFileLoader<DataSet> loader)
            {
                _processor = processor;
                _loader = loader;
            }

            public async IAsyncEnumerable<IRow> ReadEnumerableAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                DataSet dataSet = _loader.Load();

                ConcurrentBag<IRow> rowsBag = [];

                Parallel.ForEach(
                    dataSet.Tables.Cast<DataTable>(),
                    new ParallelOptions { CancellationToken = cancellationToken },
                    table => ReadWorksheet(table, rowsBag, cancellationToken));

                foreach (IRow row in rowsBag)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    yield return row;
                    await Task.Yield();
                }
            }
            public async Task<IEnumerable<IRow>> ReadTaskEnumerableAsync(CancellationToken cancellationToken = default)
            {
                //List<Row> result = [];

                //await foreach (Row row in ReadEnumerableAsync(filePath))
                //{
                //    result.Add(row);
                //}

                //return result;

                DataSet dataSet = _loader.Load();

                ConcurrentBag<IRow> rowsBag = [];
                await Task.Run(() =>
                {
                    Parallel.ForEach(
                        dataSet.Tables.Cast<DataTable>(),
                        new ParallelOptions { CancellationToken = cancellationToken },
                        table => ReadWorksheet(table, rowsBag, cancellationToken));
                }, cancellationToken)
                .ConfigureAwait(false);

                return rowsBag;
            }

            private void ReadWorksheet(DataTable table, ConcurrentBag<IRow> resultBag, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string sheetName = table.TableName;
                IReadOnlyDictionary<string, int> columnMap = GetColumnMapFromFirstRow(table);

                List<DataRow> dataRows = table.Rows.Cast<DataRow>().ToList();
                List<List<DataRow>> chunks = dataRows.ChunkRows(5000);

                Parallel.ForEach(
                    chunks,
                    new ParallelOptions { CancellationToken = cancellationToken },
                    chunk =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        foreach (DataRow? dataRow in chunk)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            Mapper.SystemDataRow source = new(dataRow, columnMap, table.Rows.IndexOf(dataRow) + 1);
                            IRow? row = _processor.Process(sheetName, source);

                            if (row != null)
                            {
                                resultBag.Add(row);
                                // Program._stopwatchLogger.Log($"Row {row.Index} - Thread {Thread.CurrentThread.ManagedThreadId}");
                            }
                        }
                    });
            }

            private static Dictionary<string, int> GetColumnMapFromFirstRow(DataTable table)
            {
                Dictionary<string, int> columnMap = new();

                if (table.Rows.Count == 0)
                    return columnMap;

                DataRow firstRow = table.Rows[0];

                for (int i = 0; i < table.Columns.Count; i++)
                {
                    object cellValue = firstRow[i];
                    string header = cellValue?.ToString()?.Trim() ?? "";

                    if (!string.IsNullOrEmpty(header))
                    {
                        columnMap[header] = i;
                    }
                }

                return columnMap;
            }
        }
    }
}
