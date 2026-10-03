using System.Text.Json;
using Shared.FileReader;

namespace Shared
{
    public class Row2 : IRow
    {
        public string WorkSheetName { get; set; } = "";
        public int Id { get; set; }
        public List<ValidationResult> ValidationErrors { get; set; } = [];
        public string Source { get; set; } = string.Empty;
        //public bool IsValid { get; set; } = true;

        public int SubscriberId { get; set; }
        public string StoreNumber { get; set; } = "";
        public string Trade { get; set; } = "";

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class Row : IRow
    {
        public string WorkSheetName { get; set; } = "";
        public int Id { get; set; }
        public List<ValidationResult> ValidationErrors { get; set; } = [];
        public string Source { get; set; } = string.Empty;
        //public bool IsValid { get; set; } = true;

        public decimal Limit { get; set; }
        public int Age { get; set; }
        public string Email { get; set; } = "";

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class Sheet1RowCreator : IRowCreatorMethod
    {
        public IRow CreateRow(string worksheetName, int index)
        {
            return new Row
            {
                WorkSheetName = worksheetName,
                Id = index
            };
        }
    }
    public class Sheet2RowCreator : IRowCreatorMethod
    {
        public IRow CreateRow(string worksheetName, int index)
        {
            return new Row2
            {
                WorkSheetName = worksheetName,
                Id = index
            };
        }
    }

    public class WorksheetStructure
    {
        public static IReadOnlyDictionary<string, (IRowCreatorMethod CreatorMethod, IReadOnlyList<IValidatorGroup> Validators)> Get()
        {
            FieldValidator.Group<decimal, Row> limitGroup = new("Limit", source => decimal.TryParse(source, out decimal value) ? value : 0m, (row, value) => row.Limit = value);
            limitGroup.Validators.Add(new FieldValidator.MandatoryValidator<decimal>());
            limitGroup.Validators.Add(new FieldValidator.PositiveDecimalValidator());
            limitGroup.Validators.Add(new FieldValidator.LimitDecimalValidator(-15, 30));

            FieldValidator.Group<int, Row> ageGroup = new("Age", source => int.TryParse(source, out int value) ? value : 0, (row, value) => row.Age = value);
            ageGroup.Validators.Add(new FieldValidator.MandatoryValidator<int>());
            ageGroup.Validators.Add(new FieldValidator.AdultValidator());

            FieldValidator.Group<string, Row> emailGroup = new("Email", source => source, (row, value) => row.Email = value);
            emailGroup.Validators.Add(new FieldValidator.MandatoryValidator<string>());
            emailGroup.Validators.Add(new FieldValidator.EmailFormatValidator());

            //	storeNumber	trade	categoryId	priority	withRecall	locationNoteHeader	autocopy_area	LNH - DispatchTextMessage	EnableDispatchText


            FieldValidator.Group<int, Row2> subscriberIdGroup = new("subscriberID", source => int.TryParse(source, out int value) ? value : 0, (row, value) => row.SubscriberId = value);
            subscriberIdGroup.Validators.Add(new FieldValidator.MandatoryValidator<int>());
            subscriberIdGroup.Validators.Add(new FieldValidator.PositiveIntValidator());
            subscriberIdGroup.Validators.Add(new FieldValidator.LimitIntValidator(1, int.MaxValue));

            Sheet1RowCreator sheet1RowCreator = new();
            Sheet2RowCreator sheet2RowCreator = new();

            return new Dictionary<string, (IRowCreatorMethod CreatorMethod, IReadOnlyList<IValidatorGroup> Validators)>
            {
                { "Sheet1", new (sheet1RowCreator, [limitGroup, ageGroup, emailGroup]) },
                //{ "Sheet2", new (sheet2RowCreator, [subscriberIdGroup]) },
                //{ "Sheet3", [limitGroup, ageGroup, emailGroup] }
            };
        }
    }
}
