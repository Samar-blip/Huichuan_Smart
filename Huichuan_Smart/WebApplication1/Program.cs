using System.Text;
using Application.AuthService;
using Application.Cache;
using Application.CaptchaService;
using Application.JwtService;
using Application.PermissionService;
using Application.Aop;
using Domain.Entity;
using Domain.Repository;
using EFCore;
using EFCore.Repositorys;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SqlSugar;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using WebApplication1.Filters;

// ==================== 创建主机构建器 ====================
var builder = WebApplication.CreateBuilder(args);

// ==================== 替换 DI 容器为 Autofac ====================
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // 注册所有 Service 层接口 → 实现，并启用 AOP 拦截
    containerBuilder.RegisterType<AuthService>().As<IAuthService>().InstancePerDependency()
        .EnableInterfaceInterceptors()
        .InterceptedBy(typeof(LogInterceptor));
    
    // 注册 LogInterceptor 本身（依赖 ILogger 和 IHttpContextAccessor）
    containerBuilder.RegisterType<LogInterceptor>();
    
    containerBuilder.RegisterType<CaptchaService>().As<ICaptchaService>().InstancePerDependency();
    containerBuilder.RegisterType<JwtService>().As<IJwtService>().SingleInstance();
    containerBuilder.RegisterType<PermissionService>().As<IPermissionService>().InstancePerDependency();

    // 注册仓储层
    containerBuilder.RegisterType<UserRepositorys>().As<ISysUserRepository>().InstancePerDependency();
    containerBuilder.RegisterType<LoginLogRepositorys>().As<ISysLoginLogRepository>().InstancePerDependency();
    containerBuilder.RegisterType<MenuRepositorys>().As<ISysMenuRepository>().InstancePerDependency();

    // 注册 SqlSugarContext
    containerBuilder.RegisterType<SqlSugarContext>().SingleInstance();
});

// 注意：Autofac 接管后，需要先注册默认 DI 服务，再通过 autofac 覆盖
builder.Services.AddMemoryCache();  // CaptchaService 依赖 IMemoryCache
builder.Services.AddHttpContextAccessor(); // LogInterceptor 需要获取客户端 IP

// ==================== 数据库配置 ====================
var connectionString = builder.Configuration["SqlServer:Connection"]
    ?? throw new InvalidOperationException("未配置数据库连接字符串，请在 appsettings.json 中设置 SqlServer:Connection");

// ==================== Redis 配置 ====================
var redisConfig = builder.Configuration.GetSection("Redis");
var redisConnection = redisConfig["Connection"];
var redisInstanceName = redisConfig.GetValue<string>("InstanceName") ?? "HS";
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = $"{redisInstanceName}_";
    });
    builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
}

// ==================== 补全默认 DI 注册（CaptchaService 依赖 IMemoryCache） ====================
builder.Services.AddMemoryCache();

// ==================== SqlSugar ====================
builder.Services.AddSingleton<ISqlSugarClient>(sp =>
{
    var config = new ConnectionConfig()
    {
        ConnectionString = connectionString,
        DbType = DbType.SqlServer,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute,
    };

    var db = new SqlSugarScope(config, db => { });

    // 全局逻辑删除过滤器：自动排除 is_deleted=1 的记录
    db.QueryFilter.Add(new TableFilterItem<SysUser>(it => it.IsDeleted == false));
    db.QueryFilter.Add(new TableFilterItem<SysRole>(it => it.IsDeleted == false));
    db.QueryFilter.Add(new TableFilterItem<SysMenu>(it => it.IsDeleted == false));
    db.QueryFilter.Add(new TableFilterItem<SysRoleMenu>(it => it.IsDeleted == false));

    // 全局审计字段拦截器：自动填充 CreateTime/UpdateTime/CreateBy/UpdateBy
    db.Aop.DataExecuting = (entity, filterModel) =>
    {
        var now = DateTime.UtcNow;
        var userName = AsyncHttpContext.UserName;

        if (entity is AuditEntity audit)
        {
            if (filterModel.OperationType == DataFilterType.InsertByObject)
            {
                audit.CreateTime = now;
                audit.UpdateTime = now;
                audit.CreateBy = userName;
                audit.UpdateBy = userName;
            }
            else if (filterModel.OperationType == DataFilterType.UpdateByObject)
            {
                audit.UpdateTime = now;
                audit.UpdateBy = userName;
            }
        }
    };

    return db;
});

// ==================== CORS 跨域配置 ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ==================== JWT 认证配置 ====================
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("未配置 JWT Secret，请在 appsettings.json 中设置 Jwt:Secret");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HuichuanMES";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HuichuanMES";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero,
    };
});

// ==================== 注册 MVC 控制器 ====================
builder.Services.AddControllers();

// ==================== Swagger ====================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Token，格式：Bearer {你的Token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

// ==================== 启动时自动建表 ====================
using (var scope = app.Services.CreateScope())
{
    var sqlSugarCtx = scope.ServiceProvider.GetRequiredService<SqlSugarContext>();
    try
    {
        sqlSugarCtx.CreateTable();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "[建表] 数据库初始化失败（可能 SQL Server 未启动或连接字符串错误），启动后部分功能不可用");
    }
}

// ==================== HTTP 请求管道 ====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DevCors");
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    AsyncHttpContext.SetUserName(context.User?.Identity?.Name);
    try
    {
        await next();
    }
    finally
    {
        AsyncHttpContext.Clear();
    }
});

app.MapControllers();
app.Run();
