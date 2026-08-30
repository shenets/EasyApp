namespace Shared.FileReader
{
    public interface IRowCreatorMethod
    {
        IRow CreateRow(string worksheetName, int index);
    }
}