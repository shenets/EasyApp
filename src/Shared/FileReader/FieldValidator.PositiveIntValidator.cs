namespace Shared.FileReader
{
    public static partial class FieldValidator
    {
        public class PositiveIntValidator : IFieldValidator<int>
        {
            public ValidationResult Validate(string sourceKey, string? cellValue, int value) => value >= 0 ? ValidationResult.Success() : ValidationResult.Failure("Limit must be equal or greathe 0");
        }
    }
}
