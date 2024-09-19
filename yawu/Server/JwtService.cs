using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Server;

public class JwtService(string secret, string issuer, string audience)
{
    private static readonly string ConnectionIdClaim = nameof(HubIdentifier.ConnectionId).ToLower();    
    
    public string GenerateToken(HubIdentifier hubIdentifier)
    {
        var claims = new[]
        {
            new Claim(ConnectionIdClaim, hubIdentifier.ConnectionId),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public HubIdentifier ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);

        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        }, out SecurityToken validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;
        var connectionId = jwtToken.Claims.First(x => x.Type == ConnectionIdClaim).Value;

        return new HubIdentifier
        {
            ConnectionId = connectionId
        };
    }
}

public class HubIdentifier
{
    public required string ConnectionId { get; init; }
}