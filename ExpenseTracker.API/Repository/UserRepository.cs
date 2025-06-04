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

            // Update login if provided and not a duplicate
            if (!string.IsNullOrWhiteSpace(newLogin) && newLogin != user.Login)
            {
                if (await _context.Users.AnyAsync(u => u.Login == newLogin && u.Id != userId))
                    return false;
                user.Login = newLogin;
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            // Save changes only if login or password was updated
            if (!string.IsNullOrWhiteSpace(newLogin) || !string.IsNullOrWhiteSpace(newPassword))
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}
