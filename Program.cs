using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SirarAuditoria.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SirarContext>(opt =>
    opt.UseSqlite("Data Source=sirar_auditoria.db"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "sirar-auditoria",
            ValidAudience = "sirar-auditoria",
            // Usa la MISMA clave embebida que AuthController usa para firmar.
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Program.JwtSecretoEmbebido))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Crea la base de datos (si no existe) y siembra los datos del incidente.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SirarContext>();
    db.Database.EnsureCreated();
    SeedData.Initialize(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Se declara aquí, como "partial", únicamente para poder referenciar
// Program.JwtSecretoEmbebido desde los controladores (ver nota D2).
public partial class Program
{
    // --- DEFECTO SEMBRADO (D2) ---
    // Un secreto de firma de tokens JAMÁS debería vivir en el código fuente.
    // Debería leerse de una variable de entorno, de appsettings.Development.json
    // (fuera del control de versiones) o de un gestor de secretos.
    // Aquí queda embebido y, por lo tanto, visible para cualquiera que tenga
    // acceso al repositorio (o que lo descompile desde el binario publicado).
    public const string JwtSecretoEmbebido = "sirar-clave-secreta-2026-no-cambiar";
}
