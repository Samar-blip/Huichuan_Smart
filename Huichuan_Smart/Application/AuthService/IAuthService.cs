using Application.Aop;
using Application.CaptchaService;

namespace Application.AuthService
{
    //认证服务接口
    public interface IAuthService
    {
        //验证码登录
        [Log("用户登录")]
        Task<AuthResultDTO> LoginByPasswordAsync(string account, string password, string captchaId, string captchaCode);

        //验证码注册
        [Log("用户注册")]
        Task<RegisterResultDTO> RegisterAsync(string userName, string password, string confirmPassword,
            string? realName, string? phoneNumber, string? email, string captchaId, string captchaCode);

        //修改密码
        [Log("修改密码")]
        Task<ChangePasswordResultDTO> ChangePasswordAsync(long userId, string oldPassword, string newPassword);
    }
}
