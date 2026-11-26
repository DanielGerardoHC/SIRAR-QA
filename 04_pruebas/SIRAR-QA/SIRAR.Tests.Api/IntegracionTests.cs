using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SIRAR.Tests.Api;

[Trait("Nivel", "Integracion")]
public class IntegracionTests
{
    // D01 · R1 prerrequisitos validados en el servidor

    [Fact(DisplayName = "D01 - Matrícula con prerrequisito reprobado es rechazada")]
    public async Task D01_MatriculaConPrerrequisitoReprobado_Rechazada()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante091, Cuentas.ClaveEstudiante);

        var resp = await client.PostAsJsonAsync("/api/matricula", new { estudianteId = 2, moduloId = 2, ciclo = "2026-C2" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("COIDS0204", body.GetProperty("prerrequisito").GetString());
    }

    [Fact(DisplayName = "D01 - Matrícula con prerrequisito aprobado es aceptada")]
    public async Task D01_MatriculaConPrerrequisitoAprobado_Aceptada()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante014, Cuentas.ClaveEstudiante);

        var resp = await client.PostAsJsonAsync("/api/matricula", new { estudianteId = 1, moduloId = 2, ciclo = "2026-C2" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact(DisplayName = "D01 - Módulo sin prerrequisitos se matricula sin restricción")]
    public async Task D01_ModuloSinPrerrequisitos_Aceptada()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante091, Cuentas.ClaveEstudiante);

        var resp = await client.PostAsJsonAsync("/api/matricula", new { estudianteId = 2, moduloId = 1, ciclo = "2026-C2" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // D02 · Control de rol en calificaciones

    [Fact(DisplayName = "D02 - Un estudiante no puede modificar calificaciones")]
    public async Task D02_EstudianteModificaNota_Prohibido()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante091, Cuentas.ClaveEstudiante);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/4", new { nota = 9.0 });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.Equal(4.2, app.ConBaseDeDatos(db => db.Inscripciones.Find(4)!.Nota));
    }

    [Fact(DisplayName = "D02 - El coordinador puede modificar calificaciones")]
    public async Task D02_CoordinadorModificaNota_Permitido()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/1", new { nota = 8.5 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact(DisplayName = "D02 - El administrador puede modificar calificaciones")]
    public async Task D02_AdministradorModificaNota_Permitido()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Administrador, Cuentas.ClaveAdministrador);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/1", new { nota = 7.0 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // D03 · Registro de auditoría

    [Fact(DisplayName = "D03 - El cambio de nota queda registrado con usuario y valor anterior")]
    public async Task D03_CambioNota_QuedaRegistrado()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        await client.PutAsJsonAsync("/api/calificaciones/6", new { nota = 4.5 });

        var registro = app.ConBaseDeDatos(db => db.RegistrosAuditoria.SingleOrDefault(r => r.Accion == "cambio_nota"));
        Assert.NotNull(registro);
        Assert.Equal(Cuentas.Coordinador, registro!.Usuario);
        Assert.Contains("Inscripcion 6", registro.Detalle);
        Assert.Contains("8", registro.Detalle);
    }

    [Fact(DisplayName = "D03 - La primera nota de una inscripción queda registrada")]
    public async Task D03_PrimeraNota_QuedaRegistrada()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/3", new { nota = 7.5 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var registro = app.ConBaseDeDatos(db => db.RegistrosAuditoria.Single(r => r.Accion == "cambio_nota"));
        Assert.Contains("sin nota", registro.Detalle);
        Assert.Equal("aprobado", app.ConBaseDeDatos(db => db.Inscripciones.Find(3)!.Estado));
    }

    [Fact(DisplayName = "D03 - La matrícula queda registrada")]
    public async Task D03_Matricula_QuedaRegistrada()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante014, Cuentas.ClaveEstudiante);

        await client.PostAsJsonAsync("/api/matricula", new { estudianteId = 1, moduloId = 2, ciclo = "2026-C2" });

        Assert.True(app.ConBaseDeDatos(db => db.RegistrosAuditoria.Any(r => r.Accion == "matricula")));
    }

    [Fact(DisplayName = "D03 - Un inicio de sesión fallido queda registrado")]
    public async Task D03_LoginFallido_QuedaRegistrado()
    {
        await using var app = new SirarApiFactory();
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/api/auth/login", new { email = Cuentas.Coordinador, password = "incorrecta" });

        Assert.True(app.ConBaseDeDatos(db => db.RegistrosAuditoria.Any(r => r.Accion == "login_fallido" && r.Usuario == Cuentas.Coordinador)));
    }

    // D04 · Clave de firma fuera del código

    [Fact(DisplayName = "D04 - Un token firmado con la clave publicada en el repositorio es rechazado")]
    public async Task D04_TokenConClavePublicada_Rechazado()
    {
        await using var app = new SirarApiFactory();
        var client = app.CreateClient();
        var token = TokenFirmadoCon("sirar-clave-secreta-2026-no-cambiar", "Coordinador");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.GetAsync("/api/matricula/estudiante/SW-2023-014");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // D05 · Contraseñas protegidas

    [Fact(DisplayName = "D05 - Las contraseñas no se almacenan en texto plano")]
    public async Task D05_ContrasenasNoEstanEnTextoPlano()
    {
        await using var app = new SirarApiFactory();
        app.CreateClient();

        var claves = app.ConBaseDeDatos(db => db.Usuarios.Select(u => u.Password).ToList());

        Assert.Equal(4, claves.Count);
        Assert.DoesNotContain(Cuentas.ClaveCoordinador, claves);
        Assert.DoesNotContain(Cuentas.ClaveAdministrador, claves);
        Assert.DoesNotContain(Cuentas.ClaveEstudiante, claves);
    }

    // D06 · Límite de intentos

    [Fact(DisplayName = "D06 - El sexto intento fallido consecutivo es bloqueado")]
    public async Task D06_IntentosRepetidos_Bloqueados()
    {
        await using var app = new SirarApiFactory();
        var client = app.CreateClient();
        HttpStatusCode ultimo = HttpStatusCode.OK;

        for (var i = 1; i <= 6; i++)
        {
            var resp = await client.PostAsJsonAsync("/api/auth/login", new { email = Cuentas.Coordinador, password = $"intento{i}" });
            ultimo = resp.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, ultimo);
    }

    // D08 · Expediente solo para su titular o el personal

    [Fact(DisplayName = "D08 - Un estudiante no puede consultar el expediente de otro")]
    public async Task D08_ExpedienteAjeno_Prohibido()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante091, Cuentas.ClaveEstudiante);

        var resp = await client.GetAsync("/api/matricula/estudiante/SW-2023-014");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact(DisplayName = "D08 - Un estudiante consulta su propio expediente")]
    public async Task D08_ExpedientePropio_Permitido()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante091, Cuentas.ClaveEstudiante);

        var resp = await client.GetAsync("/api/matricula/estudiante/SW-2023-091");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact(DisplayName = "D08 - El coordinador consulta cualquier expediente")]
    public async Task D08_CoordinadorConsultaExpediente_Permitido()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        var resp = await client.GetAsync("/api/matricula/estudiante/SW-2023-014");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact(DisplayName = "D08 - Un carnet inexistente devuelve 404")]
    public async Task D08_CarnetInexistente_NoEncontrado()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        var resp = await client.GetAsync("/api/matricula/estudiante/SW-0000-000");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // D09 · Matrícula solo a nombre propio

    [Fact(DisplayName = "D09 - Un estudiante no puede matricular a otro estudiante")]
    public async Task D09_MatriculaANombreDeOtro_Prohibida()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Estudiante091, Cuentas.ClaveEstudiante);

        var resp = await client.PostAsJsonAsync("/api/matricula", new { estudianteId = 1, moduloId = 1, ciclo = "2026-C2" });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact(DisplayName = "D09 - Matrícula en un módulo inexistente devuelve 404")]
    public async Task D09_ModuloInexistente_NoEncontrado()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        var resp = await client.PostAsJsonAsync("/api/matricula", new { estudianteId = 1, moduloId = 99, ciclo = "2026-C2" });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // D10 · R2 nota mínima de aprobación

    [Fact(DisplayName = "D10 - Una nota de 6.5 queda como reprobada")]
    public async Task D10_NotaBajoMinimo_Reprobada()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/1", new { nota = 6.5 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("reprobado", app.ConBaseDeDatos(db => db.Inscripciones.Find(1)!.Estado));
    }

    // D11 · Rango de nota

    [Theory(DisplayName = "D11 - Notas fuera de rango son rechazadas")]
    [InlineData(-1.0)]
    [InlineData(11.0)]
    public async Task D11_NotaFueraDeRango_Rechazada(double nota)
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/1", new { nota });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact(DisplayName = "D11 - Cambio de nota en una inscripción inexistente devuelve 404")]
    public async Task D11_InscripcionInexistente_NoEncontrada()
    {
        await using var app = new SirarApiFactory();
        var client = await app.ClienteConSesionAsync(Cuentas.Coordinador, Cuentas.ClaveCoordinador);

        var resp = await client.PutAsJsonAsync("/api/calificaciones/999", new { nota = 8.0 });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    private static string TokenFirmadoCon(string clave, string rol)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
        var token = new JwtSecurityToken(
            issuer: "sirar-auditoria",
            audience: "sirar-auditoria",
            claims: new[] { new Claim(ClaimTypes.Name, "no-existe@itca.edu.sv"), new Claim(ClaimTypes.Role, rol) },
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
