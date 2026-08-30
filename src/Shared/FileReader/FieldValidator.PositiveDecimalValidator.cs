namespace Shared.FileReader
{
    public static partial class FieldValidator
    {
        public class PositiveDecimalValidator : IFieldValidator<decimal>
        {
            public ValidationResult Validate(string sourceKey, string? cellValue, decimal value) => value >= 0 ? ValidationResult.Success() : ValidationResult.Failure("Limit must be equal or greathe 0");
        }
    }
}
