using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ExpenseTracker.Data.Model
{
    public class Category
    {
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string MccCodes { get; set; }
        public Guid? UserId { get; set; } // Null for built-in, user ID for custom
        public bool IsBuiltIn { get; set; } // True for built-in categories
        [NotMapped]
        public int[] MccCodesArray
        {
            get => string.IsNullOrEmpty(MccCodes) ? Array.Empty<int>() : JsonSerializer.Deserialize<int[]>(MccCodes);
            set => MccCodes = JsonSerializer.Serialize(value);
        }
    }
}