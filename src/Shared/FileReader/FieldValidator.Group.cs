namespace Shared.FileReader
{
    public interface IValidatorGroup
    {
        ValidationResult Apply(Mapper.IRowSource source, IRow target);
    }

    public static partial class FieldValidator
    {
        public class Group<T, K> : IValidatorGroup
            where K : class, IRow, new()
        {
            public string SourceKey { get; }
            public Func<string?, T?> Converter { get; }
            public Action<K, T> Assign { get; }
            public List<IFieldValidator<T>> Validators { get; } = [];

            public Group(string sourceKey, Func<string?, T?> converter, Action<K, T> assign)
            {
                SourceKey = sourceKey;
                Converter = converter;
                Assign = assign;
            }

            public ValidationResult Apply(Mapper.IRowSource source, IRow target)
            {
                Type type = target.GetType();
                if (type != typeof(K))
                    return ValidationResult.Failure("Invalid row type");

                return ApplyTyped(source, target);
                
            }

            private ValidationResult ApplyTyped(Mapper.IRowSource source, IRow target)
            {
                string? cell = source.Get(SourceKey);

                T? value = Converter(cell);
                if (value == null)
                    return ValidationResult.Failure($"Cannot convert '{SourceKey}'");

                foreach (IFieldValidator<T> validator in Validators)
                {
                    ValidationResult result = validator.Validate(SourceKey, cell, value);
                    if (!result.IsValid)
                    {
                        result.Source = cell;
                        return result;
                    }
                }

                Assign((K)target, value);

                return ValidationResult.Success();
            }
        }
    }
}
