using ExpenseTracker.API.Interface;
using ExpenseTracker.Data.Model;

namespace ExpenseTracker.API.Repository
{
    public class UserRepository: GenericRepository<User>,IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }
    }
}
