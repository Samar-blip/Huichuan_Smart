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

        public static AuthResultDTO Ok(string accessToken, string userName, string realName)
            => new() { Success = true, AccessToken = accessToken, UserName = userName, RealName = realName };

        public static AuthResultDTO Fail(string message) => new() { Success = false, Message = message };
    }
}
