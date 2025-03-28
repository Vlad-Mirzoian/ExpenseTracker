﻿using ExpenseTracker.Data.Model;

namespace ExpenseTracker.API.Interface
{
    public interface IUserRepository: IGenericRepository<User>
    {
        Task<User> GetUserByLoginAsync(string login);
    }
}
