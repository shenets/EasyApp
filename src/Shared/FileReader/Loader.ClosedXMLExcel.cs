using ClosedXML.Excel;

namespace Shared.FileReader
{
    public static partial class Loader
    {
        public class ClosedXMLExcel : IFileLoader<byte[]>
        {
            private readonly string _filePath;

            public ClosedXMLExcel(string filePath)
            {
                _filePath = filePath;
            }

            public byte[] Load()
            {
                // verify FileOptions

                byte[]? buffer = null;
                using XLWorkbook origin = new(_filePath);
                using (MemoryStream ms = new())
                {
                    origin.SaveAs(ms);
                    buffer = ms.ToArray();
                }

                return buffer;
            }
        }
    }
}
