namespace Application.AuthService
{
    //注册结果DTO
    public class RegisterResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static RegisterResultDTO Ok() => new() { Success = true };
        public static RegisterResultDTO Fail(string message) => new() { Success = false, Message = message };
    }
}
