using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ClosedXML.Excel;

namespace Shared.FileReader
{
    public static partial class Reader
    {
        public class ClosedXMLExcel : IFileReader
        {
            private readonly WorksheetProcessor _processor;
            private readonly IFileLoader<byte[]> _loader;

            public ClosedXMLExcel(WorksheetProcessor processor, IFileLoader<byte[]> loader)
            {
                _processor = processor;
                _loader = loader;
            }

            public async IAsyncEnumerable<IRow> ReadEnumerableAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                //Program._stopwatchLogger.Log($"Start read buffer -  {Thread.CurrentThread.ManagedThreadId}");

                List<string> keys = _processor.Groups.Keys.ToList();
                byte[] buffer = _loader.Load();

                //Program._stopwatchLogger.Log($"End read buffer -  {Thread.CurrentThread.ManagedThreadId}");

                ConcurrentBag<IRow> rowsBag = [];
                Parallel.ForEach(
                    Enumerable.Range(0, keys.Count),
                    new ParallelOptions { CancellationToken = cancellationToken },
                    index => ReadWorksheet($"{keys[index]}", buffer, rowsBag));

                //Program._stopwatchLogger.Log($"RowsBag prepared -  {Thread.CurrentThread.ManagedThreadId}");

                foreach (IRow row in rowsBag)
                {
                    yield return row;
                    await Task.Yield();
                }
            }
            public async Task<IEnumerable<IRow>> ReadTaskEnumerableAsync(CancellationToken cancellationToken = default)
            {
                //List<Row> result = new List<Row>();

                //await foreach (Row row in ReadEnumerableAsync(filePath))
                //{
                //    result.Add(row);
                //}

                //return result;

                byte[] buffer = _loader.Load();

                List<string> keys = _processor.Groups.Keys.ToList();
                ConcurrentBag<IRow> rowsBag = [];
                await Task.Run(() =>
                {
                    Parallel.ForEach(
                        Enumerable.Range(0, keys.Count),
                        new ParallelOptions { CancellationToken = cancellationToken },
                        index => ReadWorksheet($"{keys[index]}", buffer, rowsBag, cancellationToken));
                }, cancellationToken)
                .ConfigureAwait(false);

                return rowsBag;
            }

            private void ReadWorksheet(string worksheetName, byte[] buffer, ConcurrentBag<IRow> rows, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //Program._stopwatchLogger.Log($"Start load WorksheetName {worksheetName}  -  {Thread.CurrentThread.ManagedThreadId}");

                using MemoryStream localStream = new(buffer);
                using XLWorkbook workBook = new(localStream);
                IXLWorksheet worksheet = workBook.Worksheet(worksheetName);

                //Program._stopwatchLogger.Log($"End load WorksheetName {worksheetName}  -  {Thread.CurrentThread.ManagedThreadId}");

                IReadOnlyDictionary<string, string> columnMap = GetColumnMap(worksheet);

                List<IXLRow> data = [.. worksheet.RowsUsed()];
                List<List<IXLRow>> chunks = data.ChunkRows(5000);

                //Program._stopwatchLogger.Log($"Strart process Chunks {worksheetName}  -  {Thread.CurrentThread.ManagedThreadId}");

                Parallel.ForEach(
                    chunks,
                    new ParallelOptions { CancellationToken = cancellationToken },
                    chunk =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        foreach (IXLRow? xlRow in chunk)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            Mapper.ClosedXMLExcel source = new(xlRow, columnMap);

                            //Program._stopwatchLogger.Log($"Row {source.Index}  -  {Thread.CurrentThread.ManagedThreadId}");

                            IRow? row = _processor.Process(worksheet.Name, source);
                            if (row != null)
                                rows.Add(row);
                        }
                    });

                //Program._stopwatchLogger.Log($"End process Chunks {worksheetName}  -  {Thread.CurrentThread.ManagedThreadId}");
            }

            private static Dictionary<string, string> GetColumnMap(IXLWorksheet sheet)
            {
                IXLRow? headerRow = sheet.FirstRowUsed();
                Dictionary<string, string> columnMap = [];

                foreach (IXLCell cell in headerRow.CellsUsed())
                {
                    string header = cell.GetString().Trim();
                    string columnLetter = cell.Address.ColumnLetter;

                    if (!string.IsNullOrEmpty(header))
                        columnMap[header] = columnLetter;
                }

                return columnMap;
            }
        }
    }
}
