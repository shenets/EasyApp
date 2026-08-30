namespace Shared.FileReader
{
    public static partial class FieldValidator
    {
        public class MandatoryValidator<T> : IFieldValidator<T>
        {
            public ValidationResult Validate(string sourceKey, string? cellValue, T value) => !string.IsNullOrWhiteSpace(cellValue)  ? ValidationResult.Success() : ValidationResult.Failure($"Invalid mandatory '{sourceKey}' column");
        }
    }
}
