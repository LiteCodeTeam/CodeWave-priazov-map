﻿namespace Backend.Models
{
    public class JwtSettings
    {
        public string AccessTokenSecret { get; set; } = null!;
        public string RefreshTokenSecret { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int AccessTokenExpiryMinutes { get; set; }
        public int RefreshTokenExpiryDays { get; set; }
    }
}
