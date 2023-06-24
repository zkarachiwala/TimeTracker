using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TimeTracker.Shared.Models.Login;

namespace TimeTracker.API.Services;

public class LoginService : ILoginService
{
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _config;

    public LoginService(SignInManager<User> signInManager, IConfiguration config)
    {
        _signInManager = signInManager;
        _config = config;
    }
    public async Task<LoginResponse> Login(LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(request.UserName, request.Password, false, false);

        if(!result.Succeeded)
        {
            return new LoginResponse(false, "Email of password is wrong.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.UserName)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["JwtSecurityKey"]!)
        );
        var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.Now.AddDays(
            Convert.ToInt32(_config["JwtExpiryInDays"]));
        
        var token = new JwtSecurityToken(
            issuer: _config["JwtIssuer"],
            audience: _config["JwtAudience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponse(true, null, jwt);
    }
}