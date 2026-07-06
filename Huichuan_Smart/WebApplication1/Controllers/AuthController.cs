using Application.AuthService;
using Application.CaptchaService;
using Application.PermissionService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 认证控制器，提供登录、改密、验证码等接口
    /// </summary>
    [ApiController]
    [Route("[controller]/[action]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICaptchaService _captchaService;
        private readonly IPermissionService _permissionService;

        public AuthController(IAuthService authService, ICaptchaService captchaService, IPermissionService permissionService)
        {
            _authService = authService;
            _captchaService = captchaService;
            _permissionService = permissionService;
        }

        /// <summary>
        /// 获取图形验证码（登录页调用），返回验证码ID和Base64图片
        /// </summary>
        /// <returns>包含 captchaId 和 imageBase64 的匿名对象</returns>
        [HttpGet()]
        public IActionResult GetCaptcha()
        {
            var result = _captchaService.Generate();
            return Ok(new
            {
                captchaId = result.CaptchaId,
                imageBase64 = result.ImageBase64
            });
        }

        /// <summary>
        /// 密码登录（支持用户名或手机号），需先校验图形验证码
        /// </summary>
        /// <param name="request">登录请求体，包含账号、密码、验证码ID和验证码</param>
        /// <returns>成功返回 accessToken、userName、realName；失败返回 401</returns>
        [HttpPost()]
        public async Task<IActionResult> Login([FromBody] PasswordLoginRequest request)
        {
            var result = await _authService.LoginByPasswordAsync(
                request.Account, request.Password, request.CaptchaId, request.CaptchaCode);

            if (!result.Success)
            {
                var response = new {
                    message = result.Message,
                    isUserNotFound = result.IsUserNotFound,
                    needNewCaptcha = result.NeedNewCaptcha,
                    newCaptchaId = result.NewCaptchaId
                };
                return Unauthorized(response);
            }

            return Ok(new
            {
                accessToken = result.AccessToken,
                userName = result.UserName,
                realName = result.RealName,
            });
        }

        /// <summary>
        /// 修改密码（需登录状态）
        /// </summary>
        /// <param name="request">修改密码请求体，包含旧密码和新密码</param>
        /// <returns>成功返回提示信息；失败返回 400</returns>
        [HttpPost()]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "无效的用户凭证" });

            var result = await _authService.ChangePasswordAsync(
                userId, request.OldPassword, request.NewPassword);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = "密码修改成功" });
        }

        /// <summary>
        /// 获取当前登录用户信息（验证 Token 有效性）
        /// </summary>
        /// <returns>返回 userId 和 userName</returns>
        [HttpGet()]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;
            var userName = User.FindFirst("unique_name")?.Value;
            return Ok(new { userId, userName });
        }

        /// <summary>
        /// 获取当前登录用户的菜单树（用于前端动态渲染左侧导航）
        /// </summary>
        /// <returns>菜单树节点列表</returns>
        [HttpGet()]
        [Authorize]
        public async Task<IActionResult> GetUserMenus()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "无效的用户凭证" });

            var menus = await _permissionService.GetUserMenusAsync(userId);
            return Ok(menus);
        }

    }
}
