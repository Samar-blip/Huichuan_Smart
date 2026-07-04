namespace Application.AuthService
{
    //认证服务接口
    public interface IAuthService
    {
        //密码登录
        Task<AuthResultDTO> LoginByPasswordAsync(string account, string password);

        //修改密码
        Task<ChangePasswordResultDTO> ChangePasswordAsync(long userId, string oldPassword, string newPassword);

        //用户注册
        Task<RegisterResultDTO> RegisterAsync(string userName, string password, string confirmPassword,
            string? realName, string? phoneNumber, string? email);
    }
}
