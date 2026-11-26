using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SirarAuditoria.Data;
using SirarAuditoria.Models;
using SirarAuditoria.Services;

namespace SirarAuditoria.Controllers;

public record MatriculaRequest(int EstudianteId, int ModuloId, string Ciclo);

[ApiController]
[Route("api/matricula")]
[Authorize]
public class MatriculaController : ControllerBase
{
    private readonly SirarContext _db;
    public MatriculaController(SirarContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Matricular(MatriculaRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (User.IsInRole("Estudiante") && EstudianteDelToken() != req.EstudianteId)
            return Forbid();

        var modulo = await _db.Modulos
            .Include(m => m.Prerrequisitos)
            .FirstOrDefaultAsync(m => m.Id == req.ModuloId);
        if (modulo is null) return NotFound(new { mensaje = "Módulo no encontrado." });

        var aprobados = await _db.Inscripciones
            .Where(i => i.EstudianteId == req.EstudianteId && i.Estado == "aprobado")
            .Select(i => i.ModuloId)
            .ToListAsync();

        // Enforce R1 server-side
        var pendientes = ReglasAcademicas.PrerrequisitosPendientes(modulo, aprobados);
        if (pendientes.Count > 0)
            return UnprocessableEntity(new
            {
                mensaje = $"El módulo requiere haber aprobado {pendientes[0].Codigo} {pendientes[0].Nombre}.",
                prerrequisito = pendientes[0].Codigo,
                regla = "R1"
            });

        var inscripcion = new Inscripcion
        {
            EstudianteId = req.EstudianteId,
            ModuloId = req.ModuloId,
            Ciclo = req.Ciclo,
            Estado = "cursando"
        };
        _db.Inscripciones.Add(inscripcion);
        _db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            Fecha = DateTime.UtcNow,
            Usuario = User.Identity?.Name ?? "desconocido",
            Accion = "matricula",
            Detalle = $"Estudiante {req.EstudianteId}: modulo {modulo.Codigo} ciclo {req.Ciclo}"
        });
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

        if (User.IsInRole("Estudiante") && EstudianteDelToken() != estudiante.Id)
            return Forbid();

        return Ok(estudiante.Inscripciones.Select(i => new {
            modulo = i.Modulo!.Codigo, i.Ciclo, i.Nota, i.Estado
        }));
    }

    private int? EstudianteDelToken() =>
        int.TryParse(User.FindFirst("estudianteId")?.Value, out var id) ? id : null;
}
