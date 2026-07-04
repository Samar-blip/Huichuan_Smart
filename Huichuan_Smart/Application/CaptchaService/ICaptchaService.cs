namespace Application.CaptchaService
{
    //图形验证码服务接口
    public interface ICaptchaService
    {
        //生成验证码，返回验证码ID、明文、Base64图片
        CaptchaResultDTO Generate();

        //验证用户输入的验证码是否正确（验证后立即失效）
        bool Validate(string captchaId, string userInput);
    }
}
