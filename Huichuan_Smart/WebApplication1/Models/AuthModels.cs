using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// 密码登录请求
    /// </summary>
    public class PasswordLoginRequest
    {
        [Required(ErrorMessage = "账号不能为空")]
        public string Account { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 验证码ID（由 /Auth/GetCaptcha 返回）
        /// </summary>
        [Required(ErrorMessage = "验证码ID缺失")]
        public string CaptchaId { get; set; } = string.Empty;

        /// <summary>
        /// 用户输入的验证码
        /// </summary>
        [Required(ErrorMessage = "验证码不能为空")]
        public string CaptchaCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// 修改密码请求
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "旧密码不能为空")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "新密码不能为空")]
        [MinLength(8, ErrorMessage = "新密码长度不能少于8位")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用户注册请求
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// 用户名（3-20位，唯一）
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "用户名长度需为3-20位")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 密码（最少8位）
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [MinLength(8, ErrorMessage = "密码长度不能少于8位")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 确认密码
        /// </summary>
        [Required(ErrorMessage = "确认密码不能为空")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// 真实姓名（可选）
        /// </summary>
        [StringLength(20, ErrorMessage = "真实姓名最长20位")]
        public string? RealName { get; set; }

        /// <summary>
        /// 手机号（可选，唯一）
        /// </summary>
        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 邮箱（可选）
        /// </summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string? Email { get; set; }

        /// <summary>
        /// 验证码ID（由 /Auth/GetCaptcha 返回）
        /// </summary>
        [Required(ErrorMessage = "验证码ID缺失")]
        public string CaptchaId { get; set; } = string.Empty;

        /// <summary>
        /// 用户输入的验证码
        /// </summary>
        [Required(ErrorMessage = "验证码不能为空")]
        public string CaptchaCode { get; set; } = string.Empty;
    }
}
