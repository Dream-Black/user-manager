using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Services;

public class AuthService
{
    private readonly IConfiguration _configuration;

    private const string PrivateKeyPem = @"-----BEGIN PRIVATE KEY-----
MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCb6V38DwdICXwN
TngzXtByfQ/uujLbCeiiAI+klxATilbu3X8brO/rb7yCbUbz5wv0+ls8DDMrRGO9
XczCjrIcf0IlLXz0OeQ+A1NDFWWdHhsPSFAvUZ9FQbxKVzD/VudX6RVBKOxIqe7C
5EAcMkX123UQ1BMd6SKlmmGmZkEUR3vg6BJhEYMnKGPEPtET0lNI21hC1l87L5Y2
+J4he7UpXo30wV4NTC2wZz0fLAHUE6T5sZAovR7r/mvHDLeVPhNLhRm+1n8SEd6g
DLKsL4385Nu2MupWzjjc910IyyezMKXvn/vxGrJxhnH82aVgcfSCIkJo2K0TDfod
WEHuWwn7AgMBAAECggEAEevAFtHvZ1NXw/vKCzWRxicj5q/WWqEH8V8ZI7UNbwGO
voQNMTQum6RLSb6f0jczg6QnSn6ofwesYz7d41sdk/L4umdBHp1s48fkESjrdiTY
vWf1d4rtQLuulejxw9fMXal3/PZkOnH19Mbhq5wHuJvSF+4YiG6cweY95S+JuXfT
a4tSBurtWatDVVqpVQDWaDQVTNlx+rz4r4rmtTqzvqS+7ego5/fPnQOJRUq8uKPj
+7Vk+v18Bx0Vpb4JwCXe2tQIJvckSijcvTNr0NRDvVjbeBVR/pT6KAqUB6NhblMy
UAgv7VLmDkY06JLfKrR1Mnd9EgFb9HjBzlQv81FIEQKBgQDscpKP8ejpumDXLuii
opVvYItAQL4J1ycfnMhDEKPRYr9iaaDNz8cDjpCcmlvdI7/UCZbLoBmAowYT0sxf
iPF2q2Ryvy+9Q5mB2bayL7mIdL4g9eiz++I5Xnx21nYtgpwbZqiYh8a9hKPcN7xa
0uCMNEHvkb1kgU8PBOp5pLhGgwKBgQCozefBotDGq8xxV2Jka4XW7Jy1zBp6+Hna
U8naIHZBUlAAmYiGuHL/Wj8VZRscc630OgpdNbyhOKdvrkFhdIpcPIZd1OPIhmtZ
nLNJcN6sIjaslPe7IhoWxbgaA7noJ4TrwUKypPEEWR1WOkxMtVeAmu2ggq0b/QIp
sM/8b4UVKQKBgGz6ia6qss03R/cl+bcr9HA3MTdWH8DtV2zsmCjA/KA/QTKcuK7j
eziadvCW/Iw6M+oI2WXUzqxJdz2fxO6rcY9eg5eXous6wv/kVp5d/Md190O905lR
GP2UHQ2w3xsuvcCrWj2jJuimv9d6IOhDlZdJZrKCm6Y9KzEi9OdIEl79AoGAEYfA
p7VW5Sr2QlcG2tLnxVgxNhgKL/caAHhvH/37CfGYaVeIfCUvnCEtJ/WmCSBiUlis
tt1bx4pwqQJ5u7s0mzuV4Ky5Mxvyjg3d+KSGG246K1YVHBQAjZje86Sra9ae0TTy
zkae2QrAJzJKtjSm764nO2IWxC9USCwajWavdXECgYBRPoNcToTWg+D3i0Q8pvZx
MDZ4Y0ccCvrZXSxFqlWYfLIRPVeFTKzvxTOECBq0crJqSVItXq1CaKuAhhWJOYIh
g6wHWGeCWN0C/9ldE7MbPmglxpZzUdtotkFHVxZZjwEaQAJHCy2LhtBUwQ0tksVO
5dBxSthKTPB2/9w1X8nrzA==
-----END PRIVATE KEY-----";

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string DecryptPassword(string encryptedPassword)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(PrivateKeyPem);

        var cipher = Convert.FromBase64String(encryptedPassword);
        var plainBytes = rsa.Decrypt(cipher, RSAEncryptionPadding.Pkcs1);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        return string.Equals(HashPassword(password), storedHash, StringComparison.OrdinalIgnoreCase);
    }

    public string GenerateToken(User user)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "ProjectHub";
        var audience = _configuration["Jwt:Audience"] ?? "ProjectHub";
        var secret = _configuration["Jwt:Secret"] ?? "ProjectHub-Development-Secret-Key-2026-Long-Enough";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(2);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
