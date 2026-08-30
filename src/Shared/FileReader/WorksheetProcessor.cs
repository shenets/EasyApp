namespace Shared.FileReader
{
    public class WorksheetProcessor
    {
        private readonly IReadOnlyDictionary<string, (IRowCreatorMethod RowCreator, IReadOnlyList<object> Validators)> _groups;

        public WorksheetProcessor(IReadOnlyDictionary<string, (IRowCreatorMethod RowCreator, IReadOnlyList<object> Validators)> groups, bool useHeaderRow = false)
        {
            _groups = groups;
            UseHeaderRow = useHeaderRow;
        }

        public IReadOnlyDictionary<string, (IRowCreatorMethod RowCreator, IReadOnlyList<object> Validators)> Groups => _groups;
        public bool UseHeaderRow { get; set; }

        public IRow? Process(string worksheetName, Mapper.IRowSource source)
        {
            if (!_groups.TryGetValue(worksheetName, out (IRowCreatorMethod RowCreator, IReadOnlyList<object> Validators) groups))
                return null;

            IRow row = groups.RowCreator.CreateRow(worksheetName, source.Index);

            List<ValidationResult> validationErrors = groups.Validators
                .OfType<IValidatorGroup>()
                .Select(validator => validator.Apply(source, row))
                .ToList();

            if (validationErrors.Any(item => !item.IsValid))
                row.ValidationErrors = validationErrors.Where(item => !item.IsValid).ToList();

            return row;
        }
    }
}