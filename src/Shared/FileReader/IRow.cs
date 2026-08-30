namespace Shared.FileReader
{
    public interface IRow
    {
        string WorkSheetName { get; set; }
        int Id { get; set; }
        List<ValidationResult> ValidationErrors { get; set; }
        //bool IsValid { get; set; }

        string Serialize();
    }
}