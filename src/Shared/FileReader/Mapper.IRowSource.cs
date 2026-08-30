namespace Shared.FileReader
{
    public static partial class Mapper
    {
        public interface IRowSource
        {
            int Index { get; }
            string? Get(string key);
        }
    }
}