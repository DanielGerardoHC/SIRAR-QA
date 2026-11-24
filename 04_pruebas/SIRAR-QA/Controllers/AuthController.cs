using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SirarAuditoria.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SirarAuditoria.Controllers;

public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SirarContext _db;
    public AuthController(SirarContext db) => _db = db;

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == req.Email);

        if (usuario is null || usuario.Password != req.Password)
            return Unauthorized(new { mensaje = "Credenciales inválidas" });

        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? Program.JwtSecretoEmbebido;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new("estudianteId", usuario.EstudianteId?.ToString() ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: "sirar-auditoria",
            audience: "sirar-auditoria",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), rol = usuario.Rol });
    }
}
