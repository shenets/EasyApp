namespace Shared.FileReader
{
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string? Message { get; }
        public string? Source { get; internal set; }

        private ValidationResult(bool isValid,  string? message = null)
        {
            IsValid = isValid;
            Message = message;
        }

        public static ValidationResult Success() => new(true);
        public static ValidationResult Failure(string message) => new(false, message);
    }
}