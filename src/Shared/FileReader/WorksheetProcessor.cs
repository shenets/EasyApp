namespace Shared.FileReader
{
    public class WorksheetProcessor
    {
        private readonly IReadOnlyDictionary<string, (IRowCreatorMethod RowCreator, IReadOnlyList<IValidatorGroup> Validators)> _groups;

        public WorksheetProcessor(IReadOnlyDictionary<string, (IRowCreatorMethod RowCreator, IReadOnlyList<IValidatorGroup> Validators)> groups, bool useHeaderRow = false)
        {
            _groups = groups;
            UseHeaderRow = useHeaderRow;
        }

        public IReadOnlyDictionary<string, (IRowCreatorMethod RowCreator, IReadOnlyList<IValidatorGroup> Validators)> Groups => _groups;
        public bool UseHeaderRow { get; set; }

        public IRow? Process(string worksheetName, Mapper.IRowSource source)
        {
            if (!_groups.TryGetValue(worksheetName, out (IRowCreatorMethod RowCreator, IReadOnlyList<IValidatorGroup> Validators) groups))
                return null;

            IRow row = groups.RowCreator.CreateRow(worksheetName, source.Index);

            List<ValidationResult>? validationErrors = null;

            foreach (IValidatorGroup validator in groups.Validators)
            {
                ValidationResult result = validator.Apply(source, row);

                if (result.IsValid)
                    continue;

                validationErrors ??= [];
                validationErrors.Add(result);
            }

            if (validationErrors is not null)
                row.ValidationErrors = validationErrors;

            return row;
        }
    }
}