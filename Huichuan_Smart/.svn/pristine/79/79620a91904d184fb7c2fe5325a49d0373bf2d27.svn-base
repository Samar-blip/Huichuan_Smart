using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Application.JwtService
{
    //JWT Token 服务实现（使用 .NET 原生 JwtSecurityToken 生成）
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //生成 JWT Access Token（包含用户ID、用户名、角色ID）
        public string GenerateToken(long userId, string userName, long roleId)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName),
                new Claim("role_id", roleId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expireHours = int.TryParse(_configuration["Jwt:ExpireHours"], out var h) ? h : 2;

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "HuichuanMES",
                audience: _configuration["Jwt:Audience"] ?? "HuichuanMES",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expireHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
