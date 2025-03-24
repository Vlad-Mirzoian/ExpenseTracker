using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExpenseTracker.Data.Model
{
    public class User
    {
        public Guid Id { get; set; } = new Guid();
        [Required]
        public string Login { get; set; }

        public string PasswordHash { get; set; }

        public string Token { get; set; }  // API-ключ от Monobank
    }
}
