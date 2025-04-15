using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string MccCodes { get; set; }

    [NotMapped]
    public int[] MccCodesArray
    {
        get
        {
            if (string.IsNullOrEmpty(MccCodes))
                return Array.Empty<int>();
            try
            {
                return JsonSerializer.Deserialize<int[]>(MccCodes) ?? Array.Empty<int>();
            }
            catch
            {
                return Array.Empty<int>();
            }
        }
        set
        {
            MccCodes = value == null || value.Length == 0
                ? null
                : JsonSerializer.Serialize(value);
        }
    }
}