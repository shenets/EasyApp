using ClosedXML.Excel;

namespace Shared.FileReader
{
    public static partial class Mapper
    {
        public class ClosedXMLExcel : IRowSource
        {
            private readonly IXLRow _row;
            private readonly IReadOnlyDictionary<string, string> _map;

            public ClosedXMLExcel(IXLRow row, IReadOnlyDictionary<string, string> map)
            {
                _row = row;
                _map = map;
            }

            public int Index => _row.RowNumber();

            public string? Get(string key)
            {
                bool isExists = _map.TryGetValue(key, out string? col);

                string? value = isExists ? _row.Cell(col).GetString() : null;

                return value;
            }
        }
    }
}