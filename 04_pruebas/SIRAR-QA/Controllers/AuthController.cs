using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SirarAuditoria.Data;
using SirarAuditoria.Models;
using SirarAuditoria.Security;
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
    private readonly JwtSettings _jwt;

    public AuthController(SirarContext db, JwtSettings jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == req.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

        if (usuario is null || !ContrasenaSegura.Verificar(req.Password, usuario.Password))
        {
            await RegistrarAsync(req.Email, "login_fallido", $"IP {ip}");
            return Unauthorized(new { mensaje = "Credenciales inválidas" });
        }

        await RegistrarAsync(usuario.Email, "login_exitoso", $"IP {ip}");

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new("estudianteId", usuario.EstudianteId?.ToString() ?? "")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtSettings.Issuer,
            audience: JwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), rol = usuario.Rol });
    }

    private async Task RegistrarAsync(string usuario, string accion, string detalle)
    {
        _db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            Fecha = DateTime.UtcNow,
            Usuario = usuario,
            Accion = accion,
            Detalle = detalle
        });
        await _db.SaveChangesAsync();
    }
}
