namespace Shared.FileReader
{
    public interface IFileLoader<T>
    {
        T Load();
    }
}