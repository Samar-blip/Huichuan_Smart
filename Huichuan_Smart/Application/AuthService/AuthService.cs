using Domain.Entity;
using Domain.Repository;
using Microsoft.Extensions.Logging;

namespace Application.AuthService
{
    //认证服务实现 — 密码登录、注册、修改密码
    public class AuthService : IAuthService
    {
        private readonly ISysUserRepository _userRepo;
        private readonly ISysLoginLogRepository _loginLogRepo;
        private readonly JwtService.IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 30;

        public AuthService(
            ISysUserRepository userRepo,
            ISysLoginLogRepository loginLogRepo,
            JwtService.IJwtService jwtService,
            ILogger<AuthService> logger)
        {
            _userRepo = userRepo;
            _loginLogRepo = loginLogRepo;
            _jwtService = jwtService;
            _logger = logger;
        }

        //用户名 + 密码登录
        public async Task<AuthResultDTO> LoginByPasswordAsync(string account, string password)
        {
            // 查找用户
            var user = await _userRepo.GetByUserNameAsync(account);

            if (user == null)
            {
                await RecordLoginLog(account, 0, false, "账号不存在");
                return AuthResultDTO.Fail("账号或密码错误");
            }

            if (user.Status == 1)
            {
                await RecordLoginLog(account, user.Id, false, "账号已被停用");
                return AuthResultDTO.Fail("账号已被停用，请联系管理员");
            }

            if (user.Status == 2 || (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow))
            {
                var remainingMinutes = (int)(user.LockoutEndTime!.Value - DateTime.UtcNow).TotalMinutes;
                await RecordLoginLog(account, user.Id, false,
                    $"账号已锁定，剩余 {remainingMinutes} 分钟");
                return AuthResultDTO.Fail($"账号已锁定，请 {remainingMinutes} 分钟后重试");
            }

            // 验证密码
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isPasswordValid)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.Status = 2;
                    user.LockoutEndTime = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    _logger.LogWarning("[登录锁定] 用户 {User} 连续失败 {Attempts} 次，已锁定 {Minutes} 分钟",
                        user.UserName, user.FailedLoginAttempts, LockoutMinutes);
                }
                await _userRepo.UpdateAsync(user);

                await RecordLoginLog(account, user.Id, false,
                    $"密码错误（第 {user.FailedLoginAttempts} 次）");
                return AuthResultDTO.Fail("账号或密码错误");
            }

            // 解锁（密码正确时重置锁定状态）
            if (user.FailedLoginAttempts > 0 || user.Status == 2)
            {
                user.FailedLoginAttempts = 0;
                user.Status = 0;
                user.LockoutEndTime = null;
            }

            // 更新登录信息
            user.LastLoginTime = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);

            // 生成 Token（含角色ID，前端/后端可据此做权限判断）
            var accessToken = _jwtService.GenerateToken(user.Id, user.UserName, user.RoleId);

            // 记录登录成功日志
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
            string confirmPassword, string? realName, string? phoneNumber, string? email)
        {
            // 检查用户名是否已存在
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
                PhoneNumber = phoneNumber,
                Email = email,
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
