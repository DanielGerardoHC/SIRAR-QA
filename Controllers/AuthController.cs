using Microsoft.AspNetCore.Mvc;
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
    // --- DEFECTO SEMBRADO (D6) ---
    // No hay límite de intentos ni bloqueo temporal: este endpoint permite
    // probar contraseñas indefinidamente (ataque de fuerza bruta).
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == req.Email);

        // --- DEFECTO SEMBRADO (D5) ---
        // Comparación de contraseña en texto plano. Debería compararse un hash
        // (por ejemplo con BCrypt.Verify), nunca el valor literal.
        if (usuario is null || usuario.Password != req.Password)
            return Unauthorized(new { mensaje = "Credenciales inválidas" });

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new("estudianteId", usuario.EstudianteId?.ToString() ?? "")
        };

        // --- DEFECTO SEMBRADO (D2) ---
        // La clave de firma del token está escrita directamente en el código (Program.cs),
        // en vez de leerse de una variable de entorno o un gestor de secretos.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Program.JwtSecretoEmbebido));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "sirar-auditoria",
            audience: "sirar-auditoria",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), rol = usuario.Rol });
    }
}
