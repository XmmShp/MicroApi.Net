using MicroAPI.Sample.Models;

namespace MicroAPI.Sample.Services;

public interface IUserService
{
    Task<User> GetUserAsync(int id);

    Task<List<User>> GetAllUsersAsync();

    Task<User> CreateUserAsync(string name, int age);

    bool DeleteUser(int id);

    void Debug();
}