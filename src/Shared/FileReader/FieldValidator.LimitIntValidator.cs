namespace Shared.FileReader
{
    public static partial class FieldValidator
    {
        public class LimitIntValidator : IFieldValidator<int>
        {
            private readonly int _min;
            private readonly int _max;

            public LimitIntValidator(int min, int max)
            {
                _min = min;
                _max = max;
            }

            public ValidationResult Validate(string sourceKey, string? cellValue, int value) => 
                _min <= value && value <= _max ? ValidationResult.Success() : ValidationResult.Failure($"Limit must be beetwen {_min} and {_max}");
        }
    }
}
