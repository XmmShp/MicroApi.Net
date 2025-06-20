# Contributing Guidelines / 贡献指南

*[English](#english) | [中文](#中文)*

<a id="english"></a>
# English

Thank you for considering contributing to the MicroAPI.Net project! Here are some guidelines to help you get involved.

## Code of Conduct

Please respect all project participants and maintain a professional and friendly communication environment.

## How to Contribute

### Reporting Issues

If you find a bug or have a feature request, please submit it through GitHub Issues, ensuring that you:

1. Check if the same or similar issue already exists
2. Use a clear title to describe the issue
3. Provide detailed steps to reproduce the issue or use cases for feature requests
4. Include code examples or screenshots if possible

### Submitting Code

1. Fork the repository and create your branch
2. Write code and add tests
3. Ensure all tests pass
4. Submit your code and create a Pull Request

### Pull Request Process

1. Update your codebase to include the latest changes from the main branch
2. Follow the project's code style and naming conventions
3. Describe your changes in detail in the PR description
4. Link related issues (if any)

## Development Environment Setup

1. Clone the repository
   ```bash
   git clone https://github.com/XmmShp/MicroAPI.Net.git
   cd MicroAPI.Net
   ```

2. Build the project
   ```bash
   dotnet build
   ```

## Coding Standards

- Follow C# coding conventions
- Use meaningful variable and function names
- Add XML documentation comments for public APIs
- Keep code concise and follow the Single Responsibility Principle

## Commit Message Guidelines

Commit messages should clearly describe the changes. We recommend using the following format:

```
<type>: <description>

[optional detailed description]

[optional reference to related issues]
```

Types can be:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style adjustments (not affecting functionality)
- `refactor`: Code refactoring
- `test`: Adding or modifying tests
- `chore`: Changes to build process or auxiliary tools

For example:
```
feat: add custom route prefix support

Added ability to set route prefix in HttpFacade attribute,
allowing developers to set a base path for the entire controller.

Closes #123
```

## Release Process

Project maintainers are responsible for releases. Version numbers follow [Semantic Versioning](https://semver.org/) standards.

## License

By contributing your code, you agree that your contributions will be licensed under the project's MIT License.

---

<a id="中文"></a>
# 中文

感谢您考虑为 MicroAPI.Net 项目做出贡献！以下是一些指导方针，帮助您参与到项目中来。

## 行为准则

请尊重所有项目参与者，保持专业和友好的交流环境。

## 如何贡献

### 报告问题

如果您发现了问题或有功能请求，请通过 GitHub Issues 提交，并确保：

1. 检查是否已存在相同或相似的问题
2. 使用清晰的标题描述问题
3. 详细描述问题的复现步骤或功能请求的用例
4. 如果可能，提供代码示例或截图

### 提交代码

1. Fork 仓库并创建您的分支
2. 编写代码并添加测试
3. 确保所有测试通过
4. 提交代码并创建 Pull Request

### Pull Request 流程

1. 更新您的代码库以包含最新的主分支更改
2. 遵循项目的代码风格和命名约定
3. 在 PR 描述中详细说明您的更改
4. 链接相关的 issue（如果有）

## 开发环境设置

1. 克隆仓库
   ```bash
   git clone https://github.com/XmmShp/MicroAPI.Net.git
   cd MicroAPI.Net
   ```

2. 构建项目
   ```bash
   dotnet build
   ```

## 代码规范

- 遵循 C# 编码规范
- 使用有意义的变量和函数名
- 为公共 API 添加 XML 文档注释
- 保持代码简洁，遵循单一职责原则

## 提交信息规范

提交信息应该清晰描述更改内容，建议使用以下格式：

```
<类型>: <描述>

[可选的详细描述]

[可选的引用相关 issue]
```

类型可以是：
- `feat`: 新功能
- `fix`: 修复 bug
- `docs`: 文档更改
- `style`: 代码风格调整（不影响代码功能）
- `refactor`: 代码重构
- `test`: 添加或修改测试
- `chore`: 构建过程或辅助工具的变动

例如：
```
feat: 添加自定义路由前缀支持

增加了在 HttpFacade 特性中设置路由前缀的功能，
允许开发者为整个控制器设置基础路径。

Closes #123
```

## 发布流程

项目维护者负责版本发布。版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/) 规范。

## 许可证

通过贡献您的代码，您同意您的贡献将在项目的 MIT 许可证下发布。
