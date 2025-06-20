# MicroAPI.Net

[![NuGet](https://img.shields.io/nuget/v/MicroAPI.Net.svg)](https://www.nuget.org/packages/MicroAPI.Net/)
[![Build Status](https://github.com/XmmShp/MicroAPI.Net/actions/workflows/ci.yml/badge.svg)](https://github.com/XmmShp/MicroAPI.Net/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

MicroAPI.Net 是一个轻量级的 .NET 源代码生成器，用于从接口和外观类自动生成 ASP.NET Core 控制器。通过使用简单的特性标记，您可以快速创建 RESTful API，而无需手动编写重复的控制器代码。

## 特性

- 🚀 **零样板代码** - 从接口或外观类自动生成控制器
- 🔄 **实时生成** - 在编译时生成代码，无运行时开销
- 🛠️ **高度可配置** - 自定义控制器名称、路由和命名空间
- 📦 **轻量级** - 最小化依赖，专注于单一职责
- 🔍 **类型安全** - 利用编译时类型检查确保 API 一致性

## 安装

通过 NuGet 包管理器安装：

```bash
dotnet add package MicroAPI.Net
```

## 快速入门

### 1. 创建服务接口

```csharp
public interface IUserService
{
    Task<User> GetUserAsync(int id);
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(string name, int age);
    Task<bool> DeleteUserAsync(int id);
}
```

### 2. 实现服务

```csharp
public class UserService : IUserService
{
    // 实现接口方法
}
```

### 3. 创建外观类或使用接口标记

**选项 1: 使用外观类**

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


生成的代码：

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

**选项 2: 使用接口**

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

生成的代码：

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

## 高级配置

### 自定义控制器名称

```csharp
[HttpFacade("CustomName")]
public interface IUserService { ... }
```

### 自定义命名空间

```csharp
[HttpFacade(ControllerNamespace = "MyApp.Api.Controllers", DtoNamespace = "MyApp.Api.Models")]
public interface IUserService { ... }
```

### 自定义方法名称

```csharp
[Get(MethodName = "FindById")]
Task<User> GetUserAsync(int id);
```

## 背景与动机

随着微服务、领域驱动设计（DDD）等架构的流行，Controller 层变得越来越薄，甚至在某些微服务群中还会配置统一网关和中间件处理返回。在这些系统中，往往每个端点就是对一个 Service 的简单转发，它们不再严格遵循 RESTful 的约定，更不会充分利用 HttpStatusCode 的语义，或者说 HttpStatusCode 的语义不足以表达丰富的业务场景，这些 API 更像是 WebRPC。

在这些背景下，MicroAPI.Net 应运而生。它旨在解决以下 ASP.NET Core API 开发中的常见挑战：

1. **重复的控制器代码**：传统的 API 控制器通常包含大量重复的样板代码，这些代码仅仅是将请求转发到服务层。

2. **关注点分离**：使用标准控制器模式时，维护 API 契约和业务逻辑实现之间的清晰分离是具有挑战性的。

3. **API 一致性**：在没有标准化方法的情况下，确保多个控制器和端点之间的 API 设计一致性可能很困难。

4. **维护成本**：服务接口的更改通常需要对应的控制器代码更改，这可能导致潜在的不一致性。

MicroAPI.Net 通过从服务接口或外观类自动生成控制器代码来解决这些问题，确保 API 层准确反映服务契约，同时消除样板代码。

## 贡献

欢迎贡献！请查看 [贡献指南](CONTRIBUTING.md) 了解详情。

## 许可证

本项目采用 MIT 许可证 - 详情请参阅 [LICENSE](LICENSE) 文件。
