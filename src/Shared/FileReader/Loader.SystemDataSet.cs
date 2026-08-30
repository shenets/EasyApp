using System.Data;

namespace Shared.FileReader
{
    public static partial class Loader
    {
        public class SystemDataSet : IFileLoader<DataSet>
        {
            private readonly DataSet _dataSet;

            public SystemDataSet(DataSet dataSet)
            {
                _dataSet = dataSet;
            }

            public DataSet Load() => _dataSet;
        }
    }
}
