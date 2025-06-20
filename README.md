# MicroAPI.Net

[![NuGet](https://img.shields.io/nuget/v/MicroAPI.Net.svg)](https://www.nuget.org/packages/MicroAPI.Net/)
[![Build Status](https://github.com/XmmShp/MicroAPI.Net/actions/workflows/ci.yml/badge.svg)](https://github.com/XmmShp/MicroAPI.Net/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

*Read this in other languages: [简体中文](README-zh-cn.md)*

MicroAPI.Net is a lightweight .NET source generator that automatically creates ASP.NET Core controllers from interfaces and facade classes. With simple attribute annotations, you can quickly build RESTful APIs without writing repetitive controller code.

## Features

- 🚀 **Zero Boilerplate** - Automatically generate controllers from interfaces or facade classes
- 🔄 **Compile-time Generation** - Generate code at compile time with no runtime overhead
- 🛠️ **Highly Configurable** - Customize controller names, routes, and namespaces
- 📦 **Lightweight** - Minimal dependencies, focused on a single responsibility
- 🔍 **Type-safe** - Leverage compile-time type checking for API consistency

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package MicroAPI.Net
```

## Quick Start

### 1. Create a Service Interface

```csharp
public interface IUserService
{
    Task<User> GetUserAsync(int id);
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(string name, int age);
    Task<bool> DeleteUserAsync(int id);
}
```

### 2. Implement the Service

```csharp
public class UserService : IUserService
{
    // Implement interface methods
}
```

### 3. Create a Facade Class or Use Interface Annotations

**Option 1: Using a Facade Class**

```csharp
[HttpFacade("User")]
public class UserServiceFacade : IUserService
{
    [Get("{id}")]
    public Task<User> GetUserAsync(int id) => null!;

    [Get("")]
    public Task<List<User>> GetAllUsersAsync() => null!;

    [Post]
    public Task<User> CreateUserAsync(string name, int age) => null!;

    [Delete("{id}", MethodName = "MyDeleteUser")]
    public bool DeleteUser(int id) => false;

    public void Debug() { }
}
```

Generated code:

```csharp
// UserController.g.cs
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MicroAPI.Sample.Facades.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly MicroAPI.Sample.Services.IUserService _service;

        public UserController(MicroAPI.Sample.Services.IUserService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public System.Threading.Tasks.Task<MicroAPI.Sample.Models.User> GetUserAsync([FromRoute] int id)
            => _service.GetUserAsync(id);

        [HttpGet("")]
        public System.Threading.Tasks.Task<System.Collections.Generic.List<MicroAPI.Sample.Models.User>> GetAllUsersAsync()
            => _service.GetAllUsersAsync();

        [HttpPost("CreateUserAsync")]
        public System.Threading.Tasks.Task<MicroAPI.Sample.Models.User> CreateUserAsync([FromBody] CreateUserAsyncRequest request)
            => _service.CreateUserAsync(request.name, request.age);

        [HttpDelete("{id}")]
        public bool MyDeleteUser([FromRoute] int id)
            => _service.DeleteUser(id);

    }
}

// UserDtos.g.cs
using System;

namespace MicroAPI.Sample.Facades.Controllers
{
    public record CreateUserAsyncRequest(string name, int age);

}
```

**Option 2: Using an Interface**

```csharp
[HttpFacade(DtoNamespace = "MyApp.Api.Models")]
public interface IUserService
{
    [Get("{id}")]
    Task<User> GetUserAsync(int id);

    [Get("")]
    Task<List<User>> GetAllUsersAsync();

    [Post]
    Task<User> CreateUserAsync(string name, int age);

    [Delete("{id}", MethodName = "MyDeleteUser")]
    bool DeleteUser(int id);

    void Debug();
}
```

Generated code:

```csharp
// UserServiceController.g.cs
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using MyApp.Api.Models;

namespace MicroAPI.Sample.Services.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserServiceController : ControllerBase
    {
        private readonly MicroAPI.Sample.Services.IUserService _service;

        public UserServiceController(MicroAPI.Sample.Services.IUserService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public System.Threading.Tasks.Task<MicroAPI.Sample.Models.User> GetUserAsync([FromRoute] int id)
            => _service.GetUserAsync(id);

        [HttpGet("")]
        public System.Threading.Tasks.Task<System.Collections.Generic.List<MicroAPI.Sample.Models.User>> GetAllUsersAsync()
            => _service.GetAllUsersAsync();

        [HttpPost("CreateUserAsync")]
        public System.Threading.Tasks.Task<MicroAPI.Sample.Models.User> CreateUserAsync([FromBody] CreateUserAsyncRequest request)
            => _service.CreateUserAsync(request.name, request.age);

        [HttpDelete("{id}")]
        public bool MyDeleteUser([FromRoute] int id)
            => _service.DeleteUser(id);

    }

}

// UserServiceDtos.g.cs
using System;

namespace MyApp.Api.Models
{
    public record CreateUserAsyncRequest(string name, int age);

}
```

## Advanced Configuration

### Custom Controller Name

```csharp
[HttpFacade("CustomName")]
public interface IUserService { ... }
```

### Custom Namespaces

```csharp
[HttpFacade(ControllerNamespace = "MyApp.Api.Controllers", DtoNamespace = "MyApp.Api.Models")]
public interface IUserService { ... }
```

### Custom Method Name

```csharp
[Get(MethodName = "FindById")]
Task<User> GetUserAsync(int id);
```

## Background and Motivation

With the rise of microservices, Domain-Driven Design (DDD), and similar architectures, Controller layers have become increasingly thin. In some microservice clusters, unified gateways and middleware handle responses. In these systems, each endpoint often simply forwards requests to a Service, no longer strictly following RESTful conventions or fully utilizing HttpStatusCode semantics. In fact, HttpStatusCode semantics are often insufficient to express rich business scenarios, making these APIs more like WebRPC.

MicroAPI.Net was created to address these challenges in ASP.NET Core API development:

1. **Repetitive Controller Code**: Traditional API controllers often contain significant boilerplate code that merely forwards requests to the service layer.

2. **Separation of Concerns**: Maintaining a clear separation between API contracts and business logic implementation is challenging with standard controller patterns.

3. **API Consistency**: Ensuring consistent API design across multiple controllers and endpoints is difficult without standardized approaches.

4. **Maintenance Cost**: Changes to service interfaces typically require corresponding controller code changes, potentially leading to inconsistencies.

MicroAPI.Net addresses these issues by automatically generating controller code from service interfaces or facade classes, ensuring the API layer accurately reflects service contracts while eliminating boilerplate code.

## Contributing

Contributions are welcome! Please see the [Contributing Guidelines](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.