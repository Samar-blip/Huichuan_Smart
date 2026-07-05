using Microsoft.Extensions.Caching.Memory;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;



namespace Application.CaptchaService
{
    //图形验证码服务实现 — 使用 System.Drawing 生成 PNG 图片，验证码存入 IMemoryCache
    public class CaptchaService : ICaptchaService
    {
        private readonly IMemoryCache _cache;
        private readonly string[] _chars = "2,3,4,5,6,7,8,9,A,B,C,D,E,F,G,H,J,K,M,N,P,Q,R,S,T,U,V,W,X,Y,Z".Split(',');
        private readonly string[] _fonts = { "Arial", "Verdana", "Times New Roman" };

        private const int CodeLength = 4;
        private const int ImageWidth = 120;
        private const int ImageHeight = 40;
        private const int CacheExpireMinutes = 5;

        public CaptchaService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public CaptchaResultDTO Generate()
        {
            // 1. 生成随机验证码
            var sb = new StringBuilder();
            var rand = new Random();
            for (int i = 0; i < CodeLength; i++)
                sb.Append(_chars[rand.Next(_chars.Length)]);
            var code = sb.ToString();

            // 2. 生成图片（PNG -> Base64）
            var imageBase64 = GenerateImage(code);

            // 3. 存入内存缓存（key = captchaId）
            var captchaId = Guid.NewGuid().ToString("N");
            _cache.Set(captchaId, code, TimeSpan.FromMinutes(CacheExpireMinutes));

            return new CaptchaResultDTO
            {
                CaptchaId = captchaId,
                Code = code,
                ImageBase64 = imageBase64
            };
        }

        public bool Validate(string captchaId, string userInput)
        {
            if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(userInput))
                return false;

            // 验证码过期（缓存中不存在）视为一次验证失败，返回 false
            if (_cache.TryGetValue(captchaId, out string? cachedCode))
            {
                // 一次性消费：无论正确与否都删除
                _cache.Remove(captchaId);
                return string.Equals(cachedCode, userInput, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        //生成验证码图片，返回 Base64 字符串（不含 data:image/png;base64, 前缀）
        private string GenerateImage(string code)
        {
            using var bitmap = new Bitmap(ImageWidth, ImageHeight);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            var rand = new Random();

            // 画背景噪点
            for (int i = 0; i < 200; i++)
            {
                int x = rand.Next(ImageWidth);
                int y = rand.Next(ImageHeight);
                bitmap.SetPixel(x, y, Color.FromArgb(rand.Next(200), rand.Next(200), rand.Next(200)));
            }

            // 画字符（每个字符独立颜色 + 旋转）
            int charWidth = ImageWidth / code.Length;
            for (int i = 0; i < code.Length; i++)
            {
                var color = Color.FromArgb(rand.Next(50, 200), rand.Next(50, 200), rand.Next(50, 200));
                using var brush = new SolidBrush(color);
                using var font = new Font(_fonts[rand.Next(_fonts.Length)], rand.Next(18, 26), FontStyle.Bold);

                float angle = rand.Next(-30, 30);
                g.TranslateTransform(i * charWidth + charWidth / 2f, ImageHeight / 2f);
                g.RotateTransform(angle);
                g.DrawString(code[i].ToString(), font, brush, -charWidth / 2f, -14f);
                g.ResetTransform();
            }

            // 画干扰曲线
            using var pen = new Pen(Color.FromArgb(rand.Next(100, 200), rand.Next(100, 200), rand.Next(100, 200)), 2);
            var points = new Point[4];
            for (int i = 0; i < 4; i++)
                points[i] = new Point(rand.Next(ImageWidth), rand.Next(ImageHeight));
            g.DrawCurve(pen, points);

            // 转成 PNG -> Base64
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
