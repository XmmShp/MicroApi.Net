using MicroAPI.Sample.Models;
using MicroAPI.Sample.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

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
    [Consumes(MediaTypeNames.Multipart.FormData)]
    public bool DeleteUser(int id) => false;

    public void Debug() { }
}