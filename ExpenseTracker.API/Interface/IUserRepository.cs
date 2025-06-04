using ExpenseTracker.Data.Model;

namespace ExpenseTracker.API
{
    public interface IUserRepository: IGenericRepository<User>
    {
        Task<User> GetUserByLoginAsync(string login);
        Task<bool> UpdateUserCredentialsAsync(Guid userId, string? newLogin, string? newPassword);
    }
}
