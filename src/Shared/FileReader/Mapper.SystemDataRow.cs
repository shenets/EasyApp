using System.Data;

namespace Shared.FileReader
{
    public static partial class Mapper
    {
        public class SystemDataRow : IRowSource
        {
            private readonly DataRow _row;
            private readonly IReadOnlyDictionary<string, int> _map;

            public SystemDataRow(DataRow row, IReadOnlyDictionary<string, int> map, int index)
            {
                _row = row;
                _map = map;
                Index = index;
            }

            public int Index { get; }

            public string? Get(string columnName)
            {
                if (_map.TryGetValue(columnName, out int index))
                {
                    return _row[index]?.ToString()?.Trim();
                }

                return null;
            }

        }
    }
}