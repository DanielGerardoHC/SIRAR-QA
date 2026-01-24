using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SirarAuditoria.Data;
using SirarAuditoria.Models;

namespace SirarAuditoria.Controllers;

public record MatriculaRequest(int EstudianteId, int ModuloId, string Ciclo);

[ApiController]
[Route("api/matricula")]
[Authorize]
public class MatriculaController : ControllerBase
{
    private readonly SirarContext _db;
    public MatriculaController(SirarContext db) => _db = db;

    // NOTA DE DISEÑO (equipo SIRAR, enero 2026):
    // La validacion de prerrequisitos (regla R1) ya se hizo en el simulador de
    // matricula del frontend. Queda pendiente para una iteracion futura repetirla
    // aqui en el servidor. Prioridad baja: se asume que nadie llama a la API
    // directamente, solo a traves de la app web.
    [HttpPost]
    public async Task<IActionResult> Matricular(MatriculaRequest req)
    {
        var inscripcion = new Inscripcion
        {
            EstudianteId = req.EstudianteId,
            ModuloId = req.ModuloId,
            Ciclo = req.Ciclo,
            Estado = "cursando"
        };
        _db.Inscripciones.Add(inscripcion);
        await _db.SaveChangesAsync();
        return Ok(inscripcion);
    }

    [HttpGet("estudiante/{carnet}")]
    public async Task<IActionResult> AvancePorCarnet(string carnet)
    {
        var estudiante = await _db.Estudiantes
            .Include(e => e.Inscripciones).ThenInclude(i => i.Modulo)
            .FirstOrDefaultAsync(e => e.Carnet == carnet);
        if (estudiante is null) return NotFound();
        return Ok(estudiante.Inscripciones.Select(i => new {
            modulo = i.Modulo!.Codigo, i.Ciclo, i.Nota, i.Estado
        }));
    }
}
