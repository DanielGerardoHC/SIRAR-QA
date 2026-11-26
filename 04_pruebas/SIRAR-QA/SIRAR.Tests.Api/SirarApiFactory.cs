using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SirarAuditoria.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SIRAR.Tests.Api;

public class SirarApiFactory : WebApplicationFactory<Program>
{
    public const string SecretoPruebas = "secreto-de-pruebas-sirar-qa-2026-minimo-32-caracteres";
    private readonly string _baseDatos = "sirar-pruebas-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Secret", SecretoPruebas);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SirarContext>>();
            services.RemoveAll<SirarContext>();
            services.AddDbContext<SirarContext>(opt => opt.UseInMemoryDatabase(_baseDatos));
        });
    }

    public async Task<HttpClient> ClienteConSesionAsync(string email, string password)
    {
        var client = CreateClient();
        var token = await ObtenerTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<string> ObtenerTokenAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    public T ConBaseDeDatos<T>(Func<SirarContext, T> consulta)
    {
        using var scope = Services.CreateScope();
        return consulta(scope.ServiceProvider.GetRequiredService<SirarContext>());
    }
}

public static class Cuentas
{
    public const string Coordinador = "coordinador@itca.edu.sv";
    public const string ClaveCoordinador = "Coord2026!";
    public const string Administrador = "admin@itca.edu.sv";
    public const string ClaveAdministrador = "Admin2026!";
    public const string Estudiante091 = "sw2023091@itca.edu.sv";
    public const string Estudiante014 = "sw2023014@itca.edu.sv";
    public const string ClaveEstudiante = "estudiante123";
}
