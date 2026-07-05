namespace Application.AuthService
{
    //认证结果DTO
    public class AuthResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? UserName { get; set; }
        public string? RealName { get; set; }
        
        /// <summary>
        /// 是否账号不存在（前端可根据此字段提示用户注册）
        /// </summary>
        public bool IsUserNotFound { get; set; }

        /// <summary>
        /// 是否需要新验证码（前端可根据此字段自动刷新验证码）
        /// </summary>
        public bool NeedNewCaptcha { get; set; }

        /// <summary>
        /// 新验证码 ID（当 NeedNewCaptcha=true 时返回，前端可直接使用）
        /// </summary>
        public string? NewCaptchaId { get; set; }

        public static AuthResultDTO Ok(string accessToken, string userName, string realName)
            => new() { Success = true, AccessToken = accessToken, UserName = userName, RealName = realName };

        public static AuthResultDTO Fail(string message) => new() { Success = false, Message = message };

        public static AuthResultDTO FailNeedNewCaptcha(string message) 
            => new() { Success = false, Message = message, NeedNewCaptcha = true };

        public static AuthResultDTO FailNeedNewCaptcha(string message, string newCaptchaId) 
            => new() { Success = false, Message = message, NeedNewCaptcha = true, NewCaptchaId = newCaptchaId };
        
        public static AuthResultDTO FailUserNotFound(string account) 
            => new() { Success = false, Message = $"账号 '{account}' 不存在，请先注册", IsUserNotFound = true };
    }
}
