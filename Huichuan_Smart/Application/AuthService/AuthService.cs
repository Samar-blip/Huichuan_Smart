using Application.Aop;
using Application.Cache;
using Application.CaptchaService;
using Application.JwtService;
using Domain.Entity;
using Domain.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Application.AuthService
{
    //认证服务实现 — 密码登录、注册、修改密码
    public class AuthService : IAuthService
    {
        private readonly ISysUserRepository _userRepo;
        private readonly ISysLoginLogRepository _loginLogRepo;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;
        private readonly IRedisCacheService _redisCache;
        private readonly ICaptchaService _captchaService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 30;

        public AuthService(
            ISysUserRepository userRepo,
            ISysLoginLogRepository loginLogRepo,
            JwtService.IJwtService jwtService,
            ILogger<AuthService> logger,
            IRedisCacheService redisCache,
            ICaptchaService captchaService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepo = userRepo;
            _loginLogRepo = loginLogRepo;
            _jwtService = jwtService;
            _logger = logger;
            _redisCache = redisCache;
            _captchaService = captchaService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthResultDTO> LoginByPasswordAsync(string account, string password, string captchaId, string captchaCode)
        {
            // 1. 验证图形验证码（失败计入 Redis，限流基于 IP 而非账号）
            //    用 IP 做限流 key：恶意攻击者不会共享同一账号，但会共享同一 IP
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            var captchaFailedKey = $"captcha:failed:ip:{ip}";
            var captchaFailedCount = _redisCache.Get<int>(captchaFailedKey);

            bool captchaOk;
            try
            {
                captchaOk = _captchaService.Validate(captchaId, captchaCode);
            }
            catch
            {
                // Validate 异常（如缓存读写失败）按失败处理
                captchaOk = false;
            }

            if (!captchaOk)
            {
                captchaFailedCount++;
                _redisCache.Set(captchaFailedKey, captchaFailedCount, TimeSpan.FromMinutes(30));

                await RecordLoginLog(account, 0, false, $"验证码错误（第{captchaFailedCount}次，IP: {ip}）");
                _logger.LogWarning("[验证码] 账号 {Account} IP {Ip} 验证码验证失败，累计 {Count} 次", account, ip, captchaFailedCount);

                if (captchaFailedCount >= 5)
                {
                    _logger.LogWarning("[验证码限流] IP {Ip} 验证码连续失败 5 次，已被限流 30 分钟", ip);
                    return AuthResultDTO.Fail($"验证码错误次数过多，请 30 分钟后再试");
                }

                // 验证码失败时返回新 captchaId，让前端自动刷新
                // 直接复用已生成的验证码，避免额外请求
                var newCaptcha = _captchaService.Generate();
                return AuthResultDTO.FailNeedNewCaptcha("验证码错误", newCaptcha.CaptchaId);
            }

            // 验证码通过，清除验证码失败计数（仅在此处清除一次）
            _redisCache.Remove(captchaFailedKey);

            // 2. 查找用户
            var user = await _userRepo.GetByUserNameAsync(account);

            // 用户不存在 —— 用 Redis 做简单限流，防止恶意扫描账号（按 IP 限流）
            if (user == null)
            {
                var notExistKey = $"auth:notexist:ip:{ip}";
                var notExistCount = _redisCache.Get<int>(notExistKey) + 1;
                _redisCache.Set(notExistKey, notExistCount, TimeSpan.FromMinutes(30));

                if (notExistCount >= MaxFailedAttempts)
                {
                    await RecordLoginLog(account, 0, false, "尝试次数过多，请稍后再试");
                    return AuthResultDTO.Fail("登录失败次数过多，请30分钟后再试");
                }

                await RecordLoginLog(account, 0, false, "账号不存在");
                _logger.LogWarning("[登录] 账号 {Account} 不存在", account);
                return AuthResultDTO.FailUserNotFound(account);
            }

            // 3. 账号已被停用
            if (user.Status == 1)
            {
                await RecordLoginLog(account, user.Id, false, "账号已被停用");
                return AuthResultDTO.Fail("账号已被停用，请联系管理员");
            }

            // 4. 检查数据库锁定状态（唯一权威来源，不依赖 Redis）
            if (user.Status == 2 && user.LockoutEndTime.HasValue)
            {
                if (user.LockoutEndTime.Value > DateTime.UtcNow)
                {
                    // 仍在锁定期内 —— 拒绝登录
                    var remainingMinutes = Math.Max(1, (int)(user.LockoutEndTime.Value - DateTime.UtcNow).TotalMinutes);
                    await RecordLoginLog(account, user.Id, false,
                        $"账号已锁定，剩余 {remainingMinutes} 分钟");
                    _logger.LogWarning("[账户锁定] 用户 {User} 尝试登录，账号仍处于锁定状态，剩余 {Minutes} 分钟",
                        user.UserName, remainingMinutes);
                    return AuthResultDTO.Fail($"账号已锁定，请 {remainingMinutes} 分钟后重试");
                }
                else
                {
                    // 锁定已过期 —— 自动解锁，重置计数
                    user.Status = 0;
                    user.LockoutEndTime = null;
                    user.FailedLoginAttempts = 0;
                    await _userRepo.UpdateAsync(user);
                    _logger.LogInformation("[自动解锁] 用户 {User} 锁定已过期，自动解锁", user.UserName);
                }
            }

            // 5. 验证密码
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isPasswordValid)
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    // 达到上限，锁定账号
                    user.Status = 2;
                    user.LockoutEndTime = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    await _userRepo.UpdateAsync(user);

                    await RecordLoginLog(account, user.Id, false,
                        $"密码错误，已连续失败 {user.FailedLoginAttempts} 次，账户已锁定 {LockoutMinutes} 分钟");
                    _logger.LogWarning("[账户锁定] 用户 {User} 连续失败 {Attempts} 次，已锁定 {Minutes} 分钟",
                        user.UserName, user.FailedLoginAttempts, LockoutMinutes);
                    return AuthResultDTO.Fail($"登录失败次数过多，账户已锁定 {LockoutMinutes} 分钟");
                }
                else
                {
                    await _userRepo.UpdateAsync(user);
                    var remaining = MaxFailedAttempts - user.FailedLoginAttempts;
                    await RecordLoginLog(account, user.Id, false,
                        $"密码错误（第 {user.FailedLoginAttempts} 次，剩余 {remaining} 次机会）");
                    return AuthResultDTO.Fail($"账号或密码错误，还剩 {remaining} 次机会");
                }
            }

            // 6. 密码正确 —— 清除失败计数和锁定状态
            if (user.FailedLoginAttempts > 0 || user.Status == 2)
            {
                user.FailedLoginAttempts = 0;
                user.Status = 0;
                user.LockoutEndTime = null;
            }

            // 7. 更新登录信息
            user.LastLoginTime = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);

            // 8. 生成 Token
            var accessToken = _jwtService.GenerateToken(user.Id, user.UserName, user.RoleId);

            await RecordLoginLog(account, user.Id, true, "登录成功");

            return AuthResultDTO.Ok(accessToken, user.UserName, user.RealName);
        }

        //修改密码（需提供旧密码）
        public async Task<ChangePasswordResultDTO> ChangePasswordAsync(long userId, string oldPassword, string newPassword)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                return ChangePasswordResultDTO.Fail("用户不存在");

            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
                return ChangePasswordResultDTO.Fail("旧密码不正确");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return ChangePasswordResultDTO.Fail("新密码长度不能少于8位");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdateTime = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);

            _logger.LogInformation("[改密] 用户 {UserId} 修改密码成功", userId);
            return ChangePasswordResultDTO.Ok();
        }

        //用户注册
        public async Task<RegisterResultDTO> RegisterAsync(string userName, string password,
            string confirmPassword, string? realName, string? phoneNumber, string? email, string captchaId, string captchaCode)
        {
            // 1. 验证图形验证码
            if (!_captchaService.Validate(captchaId, captchaCode))
                return RegisterResultDTO.Fail("验证码错误或已过期");

            // 2. 检查用户名是否已存在
            var existingUser = await _userRepo.GetByUserNameAsync(userName);
            if (existingUser != null)
                return RegisterResultDTO.Fail("该用户名已被注册");

            // 检查两次密码是否一致
            if (password != confirmPassword)
                return RegisterResultDTO.Fail("两次输入的密码不一致");

            // 创建新用户（审计字段由 SqlSugar 全局拦截器自动填充）
            var user = new SysUser
            {
                UserName = userName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RealName = realName ?? "",
                PhoneNumber = phoneNumber ?? "",
                Email = email ?? "",
                RoleId = 8,  // 默认角色：普通员工
                Status = 0,
                FailedLoginAttempts = 0,
            };

            await _userRepo.InsertAsync(user);

            _logger.LogInformation("[注册] 用户 {UserName} 注册成功", userName);
            return RegisterResultDTO.Ok();
        }

        //记录登录日志
        private async Task RecordLoginLog(string account, long userId,
            bool isSuccess, string failReason)
        {
            try
            {
                var log = new SysLoginLog
                {
                    Account = account,
                    UserId = userId,
                    IsSuccess = isSuccess,
                    FailReason = failReason,
                    LoginTime = DateTime.UtcNow,
                };
                await _loginLogRepo.SaveAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[登录日志] 写入失败: {Error}", ex.Message);
            }
        }

    }
}
