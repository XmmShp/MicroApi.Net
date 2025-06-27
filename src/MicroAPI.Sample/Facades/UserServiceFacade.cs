using MicroAPI.Sample.Models;
using MicroAPI.Sample.Services;

namespace MicroAPI.Sample.Facades;

[HttpFacade("User")]
public class UserServiceFacade : IUserService
{
    [Get("{id}")]
    public Task<User> GetUserAsync(int id) => null!;

    [Get("")]
    public Task<List<User>> GetAllUsersAsync() => null!;

    [Post]
    public Task<User> CreateUserAsync(string name, int? age) => null!;

    [Delete("{id}", MethodName = "MyDeleteUser")]
    public bool DeleteUser(int id) => false;

    public void Debug() { }
}