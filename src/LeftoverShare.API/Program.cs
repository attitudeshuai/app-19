using System.Reflection;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using LeftoverShare.API.Data;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Mappings;
using LeftoverShare.API.Middleware;
using LeftoverShare.API.Repositories;
using LeftoverShare.API.Services;
using LeftoverShare.API.Services.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// ===== 创建 WebApplicationBuilder =====
var builder = WebApplication.CreateBuilder(args);

// ===== 配置 DbContext（使用 Pomelo MySQL，从环境变量读取连接字符串） =====
// 从环境变量获取 MySQL 连接字符串的各个组成部分
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "leftovershare";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "password";

// 构建连接字符串
var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPassword};";

// 注册 DbContext，使用 Pomelo MySQL 提供程序
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

// ===== 配置 JWT Settings =====
// 绑定 JWT 配置到 JwtSettings 类
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// 注册 JwtSettings 实例（用于直接注入）
var jwtSettings = new JwtSettings
{
    Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your_super_secret_key_here_at_least_32_chars_minimum",
    Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "LeftoverShare",
    Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "LeftoverShareAPI",
    ExpiryInMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES"), out var expiry) ? expiry : 120
};
builder.Services.AddSingleton(jwtSettings);

// 用代码中的 jwtSettings 覆盖 IOptions，确保 AuthService 能获取到正确的 Secret
builder.Services.PostConfigure<JwtSettings>(options =>
{
    options.Secret = jwtSettings.Secret;
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
    options.ExpiryInMinutes = jwtSettings.ExpiryInMinutes;
});

// ===== 配置 JWT Bearer 认证 =====
builder.Services.AddAuthentication(options =>
{
    // 默认使用 JWT Bearer 认证方案
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // 验证发行者
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        // 验证受众
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        // 验证密钥
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        // 验证过期时间
        ValidateLifetime = true,
        // 允许的时钟偏差（5分钟）
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

// ===== 配置 Swagger / OpenAPI（支持 JWT 认证） =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Swagger 文档基本信息
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LeftoverShare API",
        Version = "v1",
        Description = "食物分享平台 API 接口文档"
    });

    // 添加 JWT 认证支持
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT 授权认证（使用 Bearer 方案），请输入 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    // 全局应用安全要求
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // 包含 XML 注释（如果存在）
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ===== 配置 AutoMapper =====
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ===== 配置 FluentValidation =====
// 注册所有验证器
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// ===== 配置依赖注入 =====

// 添加 HTTP 上下文访问器
builder.Services.AddHttpContextAccessor();

// 注册当前用户服务
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// 注册所有 Repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISharePostRepository, SharePostRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IPickupCodeRepository, PickupCodeRepository>();
builder.Services.AddScoped<IKarmaPointRepository, KarmaPointRepository>();

// 注册 UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 注册所有 Service
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISharePostService, SharePostService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IPickupCodeService, PickupCodeService>();
builder.Services.AddScoped<IKarmaPointService, KarmaPointService>();
builder.Services.AddScoped<IStatsService, StatsService>();

// 添加控制器支持
builder.Services.AddControllers();

// 添加 CORS 支持
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===== 构建 WebApplication =====
var app = builder.Build();

// ===== 调用 DbInitializer.Initialize() 初始化数据库 =====
await DbInitializer.Initialize(app);

// ===== 配置中间件 =====

// 异常处理中间件（必须放在最前面）
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger 中间件（开发环境）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LeftoverShare API v1");
        c.RoutePrefix = "swagger";
    });
}

// HTTPS 重定向
app.UseHttpsRedirection();

// CORS 中间件
app.UseCors("AllowAll");

// 路由中间件
app.UseRouting();

// 认证中间件（必须在授权之前）
app.UseAuthentication();

// 授权中间件
app.UseAuthorization();

// 当前用户中间件
app.UseMiddleware<CurrentUserMiddleware>();

// ===== 映射所有 Controller 路由 =====
app.MapControllers();

// ===== 运行应用 =====
app.Run();
