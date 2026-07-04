using System.Text;
using Application.AuthService;
using Application.CaptchaService;
using Application.JwtService;
using Application.PermissionService;
using Domain.Entity;
using Domain.Repository;
using EFCore;
using EFCore.Repositorys;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SqlSugar;
using System.Reflection;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 注册连接数据库
            var connectionString = builder.Configuration["SqlServer:Connection"]
                ?? throw new InvalidOperationException("未配置数据库连接字符串，请在 appsettings.json 中设置 SqlServer:Connection");

            // SqlSugar
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

            // ==================== 注册仓储层（接口 → 实现） ====================
            builder.Services.AddScoped<ISysUserRepository, UserRepositorys>();
            builder.Services.AddScoped<ISysLoginLogRepository, LoginLogRepositorys>();
            builder.Services.AddScoped<ISysMenuRepository, MenuRepositorys>();

            //SqlSugarContext
            builder.Services.AddSingleton<SqlSugarContext>();

            //注册应用服务层
            builder.Services.AddMemoryCache();  // 验证码缓存
            builder.Services.AddSingleton<IJwtService, JwtService>();
            builder.Services.AddScoped<ICaptchaService, CaptchaService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPermissionService, PermissionService>();

            //JWT 认证配置
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
                    ClockSkew = TimeSpan.Zero,  // 无时钟偏移容差，Token 严格按过期时间失效
                };
            });

            //  注册 MVC 控制器
            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                // 读取本项目的 XML 注释文件，让 Swagger 显示控制器和模型的注释
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                // 添加 JWT Bearer 认证支持（Swagger 页面自带锁形按钮）
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

            // 启动时自动建表
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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // CORS 必须在认证和授权之前
            app.UseCors("DevCors");

            // 先认证再授权（顺序不可颠倒）
            app.UseAuthentication();
            app.UseAuthorization();

            // 在每次请求中设置当前用户名到 AsyncHttpContext
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
        }
    }
}
