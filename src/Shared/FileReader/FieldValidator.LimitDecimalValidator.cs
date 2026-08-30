namespace Shared.FileReader
{
    public static partial class FieldValidator
    {
        public class LimitDecimalValidator : IFieldValidator<decimal>
        {
            private readonly decimal _min;
            private readonly decimal _max;

            public LimitDecimalValidator(decimal min, decimal max)
            {
                _min = min;
                _max = max;
            }

            public ValidationResult Validate(string sourceKey, string? cellValue, decimal value) => _max >= value && value <= _max ? ValidationResult.Success() : ValidationResult.Failure($"Limit must be beetwen {_min} and {_max}");
        }
    }
}
