﻿using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace TodoApi;

public interface ITokenService
{
    // Generate a JWT token for the specified user name and admin role
    string GenerateToken(string username, bool isAdmin = false);
}

public sealed class TokenService : ITokenService
{
    private readonly string _issuer;
    private readonly SigningCredentials _jwtSigningCredentials;
    private readonly Claim[] _audiences;

    public TokenService(IAuthenticationConfigurationProvider authenticationConfigurationProvider)
    {
        var bearerSection = authenticationConfigurationProvider.GetSchemeConfiguration(JwtBearerDefaults.AuthenticationScheme);

        // An example of what the expected schema looks like
        // "Authentication": {
        //     "Schemes": {
        //       "Bearer": {
        //         "ValidAudiences": [ ],
        //         "ValidIssuer": "",
        //         "SigningKeys": [ { "Issuer": .., "Value": base64Key, "Length": 32 } ]
        //       }
        //     }
        //   }

        var section = bearerSection.GetSection("SigningKeys:0");

        var validIssuer = bearerSection["ValidIssuer"];
        _issuer = validIssuer ?? "dotnet-user-jwts";
        var value = section["Value"];
        var signingKeyBase64 = value ?? "DCmbsZQIbC0VT3xssf+mkrAhpnVPw/x38jYliIg7Oas=";

        var signingKeyBytes = Convert.FromBase64String(signingKeyBase64);

        _jwtSigningCredentials = new SigningCredentials(new SymmetricSecurityKey(signingKeyBytes),
                SecurityAlgorithms.HmacSha256Signature);

        _audiences = bearerSection.GetSection("ValidAudiences").GetChildren()
                    .Where(s => !string.IsNullOrEmpty(s.Value))
                    .Select(s => new Claim(JwtRegisteredClaimNames.Aud, s.Value!))
                    .ToArray();
    }

    public string GenerateToken(string username, bool isAdmin = false)
    {
        var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);

        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, username));

        // REVIEW: Check that this logic is OK for jti claims
        var id = Guid.NewGuid().ToString().GetHashCode().ToString("x", CultureInfo.InvariantCulture);

        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, id));

        identity.AddClaims(_audiences);

        var handler = new JwtSecurityTokenHandler();

        var jwtToken = handler.CreateJwtSecurityToken(
            _issuer,
            audience: null,
            identity,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(30),
            issuedAt: DateTime.UtcNow,
            _jwtSigningCredentials);

        return handler.WriteToken(jwtToken);
    }
}
