namespace Shared.FileReader
{
    public static partial class FieldValidator
    {
        public class AdultValidator : IFieldValidator<int>
        {
            public ValidationResult Validate(string sourceKey, string? cellValue, int value) => value >= 18 ? ValidationResult.Success() : ValidationResult.Failure("Age must be ≥ 18");
        }
    }
}
