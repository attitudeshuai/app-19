# LeftoverShare - 本地剩饭剩菜分享平台

> 一个超本地化的剩余食物分享平台，减少食物浪费并增进邻里关系

## ✨ 功能亮点

- **本社区、短时效、无偿/低价的分享模式**
  - 专注于社区范围内的食物分享，减少运输成本
  - 短时效机制确保食物新鲜度
  - 支持无偿赠送或低价转让

- **食物照片展示和领取码安全验证**
  - 支持上传多张食物照片，真实展示食物状态
  - 6位数字领取码，确保取餐安全
  - 领取码24小时有效，过期自动失效

- **爱心积分系统，鼓励分享行为**
  - 分享食物获得爱心积分
  - 积分可用于兑换社区福利
  - 积分排行榜激励用户参与

- **完善的权限控制和状态流转**
  - 基于JWT的身份认证
  - 分享者、领取者、管理员角色分离
  - 严格的数据访问权限控制
  - 清晰的业务状态流转机制

- **Docker 一键部署，开箱即用**
  - Docker Compose 一键启动
  - 包含 MySQL 数据库和应用服务
  - 自动初始化数据库结构

## 🛠️ 技术栈

- **后端框架**: .NET Core 8.0
- **数据库**: MySQL 8.0
- **ORM**: Entity Framework Core 8.0
- **认证**: JWT (JSON Web Token)
- **密码加密**: BCrypt
- **对象映射**: AutoMapper
- **参数验证**: FluentValidation
- **API文档**: Swagger / OpenAPI
- **容器化**: Docker & Docker Compose

## 📁 目录结构

```
app-19/
├── src/
│   └── LeftoverShare.API/          # Web API 主项目
│       ├── Controllers/            # API 控制器
│       ├── Data/                   # 数据访问层
│       │   ├── Entities/           # 数据库实体
│       │   ├── AppDbContext.cs     # 数据库上下文
│       │   └── DbInitializer.cs    # 数据库初始化器
│       ├── DTOs/                   # 数据传输对象
│       │   ├── Auth/               # 认证相关 DTO
│       │   ├── SharePosts/         # 分享帖相关 DTO
│       │   ├── Reservations/       # 预约相关 DTO
│       │   ├── PickupCodes/        # 取餐码相关 DTO
│       │   ├── KarmaPoints/        # 积分相关 DTO
│       │   ├── Stats/              # 统计相关 DTO
│       │   └── Common/             # 通用 DTO
│       ├── Repositories/           # 仓库模式实现
│       ├── Services/               # 业务逻辑服务
│       ├── Helpers/                # 辅助工具类
│       ├── Middleware/             # 中间件
│       ├── Program.cs              # 应用入口
│       └── appsettings.json        # 配置文件
├── tests/
│   └── LeftoverShare.Tests/        # 单元测试项目
├── docs/                           # 项目文档
│   ├── functional_intro.md         # 功能介绍文档
│   └── api_list.md                 # API 接口列表
├── docker-compose.yml              # Docker Compose 配置
├── LeftoverShare.sln               # Visual Studio 解决方案
└── README.md                       # 项目说明文档
```

## 🚀 快速启动

### 前置要求

- Docker 20.10+
- Docker Compose 2.0+

### 启动步骤

1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd app-19
   ```

2. **启动服务**
   ```bash
   docker-compose up -d
   ```

3. **等待服务启动**
   - MySQL 数据库初始化需要约 30 秒
   - 应用服务会自动等待数据库就绪后启动

4. **访问接口**
   - API 地址: http://localhost:8089
   - Swagger 文档: http://localhost:8089/swagger

### 停止服务

```bash
docker-compose down
```

如需保留数据库数据，不要添加 `-v` 参数。如需完全清理：

```bash
docker-compose down -v
```

## 📚 API 文档

启动服务后，访问 Swagger UI 查看完整的 API 文档：

- **地址**: http://localhost:8089/swagger
- **格式**: OpenAPI 3.0
- **功能**: 
  - 查看所有接口说明
  - 在线调试接口
  - 导出 API 规范

详细的 API 列表请参考 [docs/api_list.md](docs/api_list.md)。

## 🧪 测试方式

### 方式一：Postman 集合导入

1. 启动服务后，访问 http://localhost:8089/swagger/v1/swagger.json
2. 下载 Swagger JSON 文件
3. 打开 Postman，选择 Import
4. 导入下载的 JSON 文件
5. 配置环境变量 `baseUrl` 为 `http://localhost:8089`
6. 开始测试接口

### 方式二：运行单元测试

```bash
cd tests/LeftoverShare.Tests
dotnet test
```

或者在项目根目录运行：

```bash
dotnet test
```

## 🤝 贡献与许可

### 贡献指南

1. Fork 本项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

### 代码规范

- 遵循 C# 官方编码规范
- 使用 PascalCase 命名类名和方法名
- 使用 camelCase 命名局部变量和参数
- 为公共 API 添加 XML 文档注释
- 提交前确保所有测试通过

### 许可协议

本项目采用 MIT 许可协议。详见 [LICENSE](LICENSE) 文件。

---

**让我们一起减少食物浪费，共建美好社区！** 🍽️💚
