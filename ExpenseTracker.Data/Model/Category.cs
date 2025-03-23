using System.ComponentModel.DataAnnotations.Schema;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string MccCodes { get; set; } // Храним MCC как строку (например, "5814,5815")

    [NotMapped]
    public List<int> MccCodeList
    {
        get => MccCodes?.Split(',').Select(int.Parse).ToList() ?? new List<int>();
        set => MccCodes = string.Join(",", value);
    }

}