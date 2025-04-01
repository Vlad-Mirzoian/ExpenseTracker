using System.ComponentModel.DataAnnotations.Schema;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int[] MccCodes { get; set; } // Храним MCC как строку (например, "5814,5815")
}