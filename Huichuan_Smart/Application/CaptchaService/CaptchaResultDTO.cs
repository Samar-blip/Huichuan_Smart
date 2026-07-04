namespace Application.CaptchaService
{
    //验证码生成结果DTO
    public class CaptchaResultDTO
    {
        //验证码ID（前端提交时一起传回）
        public string CaptchaId { get; set; } = "";

        //验证码明文（仅用于开发调试，生产环境不返回）
        public string Code { get; set; } = "";

        //Base64 图片字符串（image/png）
        public string ImageBase64 { get; set; } = "";
    }
}
