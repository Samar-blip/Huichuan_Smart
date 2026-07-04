namespace Application.AuthService
{
    //修改密码结果DTO
    public class ChangePasswordResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ChangePasswordResultDTO Ok() => new() { Success = true };
        public static ChangePasswordResultDTO Fail(string message) => new() { Success = false, Message = message };
    }
}
