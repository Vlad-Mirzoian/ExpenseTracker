using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ExpenseTracker.Data.Model
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string MccCodes { get; set; }
        public bool IsBuiltIn { get; set; }

        [NotMapped]
        public int[] MccCodesArray
        {
            get => string.IsNullOrEmpty(MccCodes) ? Array.Empty<int>() : JsonSerializer.Deserialize<int[]>(MccCodes);
            set => MccCodes = JsonSerializer.Serialize(value);
        }
    }
}