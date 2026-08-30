using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class FileReader2
    {
        //public async Task<string> ReadLines(string path)
        //{
        //    StringBuilder content = new StringBuilder();

        //    using var reader = new StreamReader(path);
        //    while (!reader.EndOfStream)
        //    {
        //        string? line = await reader.ReadLineAsync();
        //        if (line != null)
        //            content.AppendLine(line);

        //        //System.Threading.Thread.Sleep(500);
        //    }

        //    return content.ToString();
        //}

        public async IAsyncEnumerable<string> ReadLinesAsync(string path)
        {
            using StreamReader reader = new(path);
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line != null)
                    yield return line;
            }
        }
    }
}
