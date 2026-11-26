using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SIRAR.Tests.Api;

[Trait("Nivel", "Sistema")]
public class SistemaTests
{
    [Fact(DisplayName = "Sistema - Flujo completo de matrícula válida y consulta del avance")]
    public async Task FlujoMatriculaYConsulta()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante014, Cuentas.ClaveEstudiante);

        var alta = await client.PostAsJsonAsync("/api/matricula", new { estudianteId = 1, moduloId = 2, ciclo = "2026-C2" });
        var avance = await client.GetFromJsonAsync<JsonElement>("/api/matricula/estudiante/SW-2023-014");

        Assert.Equal(HttpStatusCode.OK, alta.StatusCode);
        Assert.Contains(avance.EnumerateArray(), i =>
            i.GetProperty("modulo").GetString() == "COIDS0704" && i.GetProperty("estado").GetString() == "cursando");
    }

    [Theory(DisplayName = "Sistema - Las operaciones protegidas exigen sesión")]
    [InlineData("GET", "/api/matricula/estudiante/SW-2023-014")]
    [InlineData("POST", "/api/matricula")]
    [InlineData("PUT", "/api/calificaciones/1")]
    public async Task OperacionesProtegidas_SinSesion(string metodo, string ruta)
    {
        await using var app = new SirarApiFactory();
        var client = app.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        if (metodo != "GET") request.Content = JsonContent.Create(new { nota = 8.0, estudianteId = 1, moduloId = 1, ciclo = "2026-C2" });

        var resp = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact(DisplayName = "Sistema - El catálogo de módulos es público y completo")]
    public async Task CatalogoPublico()
    {
        await using var app = new SirarApiFactory();
        var modulos = await app.CreateClient().GetFromJsonAsync<JsonElement>("/api/modulos");

        Assert.Equal(5, modulos.GetArrayLength());
    }

    [Fact(DisplayName = "Sistema - Los prerrequisitos de un módulo se consultan por código")]
    public async Task PrerrequisitosPorCodigo()
    {
        await using var app = new SirarApiFactory();
        var client = app.CreateClient();

        var prerrequisitos = await client.GetFromJsonAsync<JsonElement>("/api/modulos/COIDS0704/prerrequisitos");
        var inexistente = await client.GetAsync("/api/modulos/XXXX/prerrequisitos");

        Assert.Equal("COIDS0204", prerrequisitos[0].GetProperty("codigo").GetString());
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    [Theory(DisplayName = "D15 - Las respuestas incluyen cabeceras de seguridad")]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("Cross-Origin-Resource-Policy", "same-origin")]
    [InlineData("X-Frame-Options", "DENY")]
    public async Task D15_CabecerasDeSeguridad(string cabecera, string valor)
    {
        await using var app = new SirarApiFactory();
        var resp = await app.CreateClient().GetAsync("/api/modulos");

        Assert.True(resp.Headers.TryGetValues(cabecera, out var valores));
        Assert.Equal(valor, valores!.Single());
    }

    [Fact(DisplayName = "Sistema - Credenciales correctas entregan sesión y las incorrectas no")]
    public async Task InicioDeSesion()
    {
        await using var app = new SirarApiFactory();
        var client = app.CreateClient();

        var ok = await client.PostAsJsonAsync("/api/auth/login", new { email = Cuentas.Coordinador, password = Cuentas.ClaveCoordinador });
        var mal = await client.PostAsJsonAsync("/api/auth/login", new { email = Cuentas.Coordinador, password = "incorrecta" });
        var desconocido = await client.PostAsJsonAsync("/api/auth/login", new { email = "nadie@itca.edu.sv", password = "x" });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, mal.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, desconocido.StatusCode);
    }
}

[Trait("Nivel", "Aceptacion")]
public class AceptacionTests
{
    [Fact(DisplayName = "Aceptación - La secuencia del incidente del 13 de marzo ya no es posible")]
    public async Task SecuenciaDelIncidente_NoEsPosible()
    {
        await using var app = new SirarApiFactory();
        var client = app.CreateClient();

        var token = await SirarApiFactory.ObtenerTokenAsync(client, Cuentas.Estudiante091, Cuentas.ClaveEstudiante);

        var intentos = new List<HttpStatusCode>();
        for (var i = 1; i <= 12; i++)
        {
            var resp = await client.PostAsJsonAsync("/api/auth/login", new { email = Cuentas.Coordinador, password = $"intento{i}" });
            intentos.Add(resp.StatusCode);
        }

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var inscripcionesAntes = app.ConBaseDeDatos(db => db.Inscripciones.Count());

        var matricula = await client.PostAsJsonAsync("/api/matricula", new { estudianteId = 2, moduloId = 2, ciclo = "2026-C1" });
        var cambioNota = await client.PutAsJsonAsync("/api/calificaciones/6", new { nota = 8.0 });

        Assert.Contains(HttpStatusCode.TooManyRequests, intentos);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, matricula.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cambioNota.StatusCode);
        Assert.Equal(inscripcionesAntes, app.ConBaseDeDatos(db => db.Inscripciones.Count()));
    }

    [Fact(DisplayName = "Aceptación - Coordinación corrige una nota y el cambio queda trazado")]
    public async Task CoordinacionCorrigeNota_ConTrazabilidad()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/6", new { nota = 4.5 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var inscripcion = app.ConBaseDeDatos(db => db.Inscripciones.Find(6)!);
        Assert.Equal(4.5, inscripcion.Nota);
        Assert.Equal("reprobado", inscripcion.Estado);
        Assert.True(app.ConBaseDeDatos(db => db.RegistrosAuditoria.Any(r => r.Accion == "cambio_nota" && r.Usuario == Cuentas.Coordinador)));
    }

    [Fact(DisplayName = "Aceptación - Un estudiante con todos los prerrequisitos avanza en su ruta")]
    public async Task EstudianteConPrerrequisitos_Avanza()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante014, Cuentas.ClaveEstudiante);

        var resp = await client.PostAsJsonAsync("/api/matricula", new { estudianteId = 1, moduloId = 2, ciclo = "2026-C2" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
