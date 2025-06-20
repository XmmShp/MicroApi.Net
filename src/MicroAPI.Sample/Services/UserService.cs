using MicroAPI.Sample.Models;
using System.Collections.Concurrent;

namespace MicroAPI.Sample.Services;

public class UserService : IUserService
{
    private static readonly ConcurrentBag<User> Users = new();

    public Task<User> GetUserAsync(int id)
    {
        return Task.FromResult(Users.FirstOrDefault(user => user.Id == id)!);
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        return Task.FromResult(Users.ToList());
    }

    public Task<User> CreateUserAsync(string name, int age)
    {
        var id = Users.Count + 1;
        var newUser = new User
        {
            Id = id,
            Age = age,
            Name = name
        };
        Users.Add(newUser);
        return Task.FromResult(newUser);
    }

    public bool DeleteUser(int id)
    {
        throw new NotImplementedException();
    }

    public void Debug()
    {
        Console.WriteLine("Debug Here!");
    }
}
