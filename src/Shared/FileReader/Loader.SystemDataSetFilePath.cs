using System.Data;
using ExcelDataReader;

namespace Shared.FileReader
{
    public static partial class Loader
    {
        public class SystemDataSetFilePath : IFileLoader<DataSet>
        {
            private readonly bool _useHeaderRow;
            private readonly string _filePath;

            public SystemDataSetFilePath(string filePath, bool UseHeaderRow)
            {
                _useHeaderRow = UseHeaderRow;
                _filePath = filePath;
            }

            public DataSet Load()
            {
                // verify FileOptions

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using FileStream stream = File.Open(_filePath, FileMode.Open, FileAccess.Read);
                using IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream);

                DataSet dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = _useHeaderRow
                    }
                });

                return dataSet;
            }
        }
    }
}
