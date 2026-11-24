using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SirarAuditoria.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SIRAR.Tests;

public class SirarTests
{
    private static WebApplicationFactory<Program> BuildFactory(bool lowRateLimit = false)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<SirarContext>();
                services.RemoveAll<DbContextOptions<SirarContext>>();
                services.RemoveAll<DbContextOptions>();

                var uniqueDb = "test-" + Guid.NewGuid();
                services.AddDbContext<SirarContext>(opt =>
                    opt.UseInMemoryDatabase(uniqueDb));

                if (lowRateLimit)
                {
                    services.RemoveAll<Microsoft.AspNetCore.RateLimiting.IRateLimiterPolicy<string>>();
                }
            });
        });
    }

    private static async Task<string> ObtenerTokenAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    // ── D1: Ausencia de validación de prerrequisitos en el servidor ─────────

    [Fact(DisplayName = "D1a - Inscripción con prerrequisito incumplido es rechazada con 422")]
    public async Task D1a_MatriculaSinPrerrequisitoAprobado_Rechazada()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var token = await ObtenerTokenAsync(client, "sw2023091@itca.edu.sv", "estudiante123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsJsonAsync("/api/matricula",
            new { estudianteId = 2, moduloId = 2, ciclo = "2026-C1" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("prerrequisito", out _),
            "La respuesta debe indicar qué prerrequisito falta.");
    }

    [Fact(DisplayName = "D1b - Estudiante con prerrequisito aprobado puede matricularse")]
    public async Task D1b_MatriculaConPrerrequisitoAprobado_Permitida()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var token = await ObtenerTokenAsync(client, "sw2023014@itca.edu.sv", "estudiante123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsJsonAsync("/api/matricula",
            new { estudianteId = 1, moduloId = 2, ciclo = "2026-C1" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact(DisplayName = "D1c - Módulo sin prerrequisitos se inscribe libremente")]
    public async Task D1c_ModuloSinPrerrequisitos_InscripcionLibre()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var token = await ObtenerTokenAsync(client, "sw2023091@itca.edu.sv", "estudiante123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsJsonAsync("/api/matricula",
            new { estudianteId = 2, moduloId = 1, ciclo = "2027-C1" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── D3: Endpoint de calificaciones sin restricción de rol ───────────────

    [Fact(DisplayName = "D3a - Estudiante no puede modificar calificaciones")]
    public async Task D3a_EstudianteNoPuedeModificarCalificacion()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var token = await ObtenerTokenAsync(client, "sw2023091@itca.edu.sv", "estudiante123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/4", new { nota = 9.0 });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact(DisplayName = "D3b - Coordinador puede modificar calificaciones")]
    public async Task D3b_CoordinadorPuedeModificarCalificacion()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var token = await ObtenerTokenAsync(client, "coordinador@itca.edu.sv", "Coord2026!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/1", new { nota = 8.5 });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact(DisplayName = "D3c - Administrador puede modificar calificaciones")]
    public async Task D3c_AdminPuedeModificarCalificacion()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var token = await ObtenerTokenAsync(client, "admin@itca.edu.sv", "Admin2026!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/1", new { nota = 7.0 });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── D4: Ausencia de registro de auditoría en cambios de nota ───────────

    [Fact(DisplayName = "D4 - Cambio de calificación por coordinador genera registro de auditoría")]
    public async Task D4_CambioCalificacion_GeneraRegistroAuditoria()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var token = await ObtenerTokenAsync(client, "coordinador@itca.edu.sv", "Coord2026!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PutAsJsonAsync("/api/calificaciones/2", new { nota = 7.5 });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SirarContext>();
        var registros = db.RegistrosAuditoria.Where(r => r.Accion == "cambio_nota").ToList();

        Assert.NotEmpty(registros);
    }

    // ── D6: Sin límite de intentos de inicio de sesión ──────────────────────

    [Fact(DisplayName = "D6 - Seis intentos fallidos consecutivos retornan 429")]
    public async Task D6_IntentosFallidosConsecutivos_Bloqueados()
    {
        await using var factory = BuildFactory(lowRateLimit: false);
        var client = factory.CreateClient();
        HttpStatusCode? lastCode = null;
        for (int i = 0; i < 6; i++)
        {
            var resp = await client.PostAsJsonAsync("/api/auth/login",
                new { email = "coordinador@itca.edu.sv", password = "incorrecta" + i });
            lastCode = resp.StatusCode;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, lastCode);
    }

    // ── D7: Nota fuera de rango ──────────────────────────────────────────────

    [Fact(DisplayName = "D7a - Nota negativa es rechazada con 400")]
    public async Task D7a_NotaNegativa_Rechazada()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var token = await ObtenerTokenAsync(client, "coordinador@itca.edu.sv", "Coord2026!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/1", new { nota = -1.0 });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact(DisplayName = "D7b - Nota mayor a 10 es rechazada con 400")]
    public async Task D7b_NotaMayorADiez_Rechazada()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var token = await ObtenerTokenAsync(client, "coordinador@itca.edu.sv", "Coord2026!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/1", new { nota = 11.0 });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Pruebas de regresión ─────────────────────────────────────────────────

    [Fact(DisplayName = "REG - Login exitoso retorna token")]
    public async Task Regresion_LoginExitoso()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "coordinador@itca.edu.sv", password = "Coord2026!" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("token", out _));
    }

    [Fact(DisplayName = "REG - Login con credenciales incorrectas retorna 401")]
    public async Task Regresion_LoginFallido()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "coordinador@itca.edu.sv", password = "incorrecta" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact(DisplayName = "REG - GET /api/modulos retorna la lista completa")]
    public async Task Regresion_ListadoModulos()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/modulos");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetArrayLength() >= 5);
    }

    [Fact(DisplayName = "REG - Avance de estudiante no accesible sin autenticación")]
    public async Task Regresion_AvanceRequiereAutenticacion()
    {
        await using var factory = BuildFactory();
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/matricula/estudiante/SW-2023-014");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
