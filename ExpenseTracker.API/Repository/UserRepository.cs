using ExpenseTracker.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API
{
    public class UserRepository: GenericRepository<User>,IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User> GetUserByLoginAsync(string login)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        }

        public async Task<bool> UpdateUserCredentialsAsync(Guid userId, string? newLogin, string? newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            if (!string.IsNullOrWhiteSpace(newLogin) && newLogin != user.Login)
            {
                if (await _context.Users.AnyAsync(u => u.Login == newLogin && u.Id != userId))
                    return false;
                user.Login = newLogin;
            }

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            if (!string.IsNullOrWhiteSpace(newLogin) || !string.IsNullOrWhiteSpace(newPassword))
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}
