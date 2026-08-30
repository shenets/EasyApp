namespace Shared.FileReader
{
    public static partial class FieldValidator
    {
        public interface IFieldValidator<T>
        {
            ValidationResult Validate(string sourceKey, string? cellValue, T value);
        }
    }
}