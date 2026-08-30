namespace Shared.FileReader
{
    public static partial class FieldValidator
    {
        public class EmailFormatValidator : IFieldValidator<string>
        {
            public ValidationResult Validate(string sourceKey, string? cellValue, string value) => value.Contains("@") ? ValidationResult.Success() : ValidationResult.Failure("Invalid email");
        }
    }
}
