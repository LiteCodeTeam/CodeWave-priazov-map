using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataBase.Models;
using Microsoft.IdentityModel.Tokens;

namespace Backend
{
    public class TokenService
    {
        private readonly string _accessTokenSecret;
        private readonly string _refreshTokenSecret;
        private readonly IConfigurationSection _jwtSettings;

        public TokenService(string accessTokenSecret, string refreshTokenSecret, IConfigurationSection jwtSettings)
        {
            _accessTokenSecret = accessTokenSecret;
            _refreshTokenSecret = refreshTokenSecret;
            _jwtSettings = jwtSettings;
        }

        // Валидация токена (для Access и Refresh)
        public ClaimsPrincipal? ValidateToken(string token, bool isAccessToken)
        {
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                isAccessToken ? _accessTokenSecret : _refreshTokenSecret));

            var validator = new JwtSecurityTokenHandler();
            try
            {
                var principal = validator.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public string GenerateAccessToken(string userId, string email, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_accessTokenSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
            }),
                Issuer = _jwtSettings["Issuer"],
                Audience = _jwtSettings["Audience"],
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtSettings["AccessTokenExpiryMinutes"])),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Генерация Refresh Token (обычно случайная строка, но может быть и JWT)
        public string GenerateRefreshToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_refreshTokenSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }),
                Issuer = _jwtSettings["Issuer"],
                Audience = _jwtSettings["Audience"],
                Expires = DateTime.UtcNow.AddDays(Convert.ToDouble(_jwtSettings["RefreshTokenExpiryDays"])),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public ClaimsPrincipal? ValidateExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                // Валидация без проверки срока действия
                return tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_refreshTokenSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings["Audience"],
                    ValidateLifetime = false
                }, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}

