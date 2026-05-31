using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EvaluaT.Application.Auth;
using EvaluaT.Domain.Auth;
using Microsoft.IdentityModel.Tokens;

namespace EvaluaT.Api.Auth;

public sealed class JwtTokenGenerator : ITokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Generate(UserAccount userAccount)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "EvaluaT";
        var audience = _configuration["Jwt:Audience"] ?? "EvaluaT.Web";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userAccount.Id.ToString()),
            new(ClaimTypes.NameIdentifier, userAccount.Id.ToString()),
            new(ClaimTypes.Name, userAccount.FullName),
            new(ClaimTypes.Email, userAccount.Email),
            new(ClaimTypes.Role, userAccount.Role.ToString())
        };

        if (userAccount.StudentId is not null)
        {
            claims.Add(new Claim("studentId", userAccount.StudentId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
