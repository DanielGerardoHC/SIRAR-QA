using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SirarAuditoria.Data;
using SirarAuditoria.Models;
using SirarAuditoria.Services;

namespace SirarAuditoria.Controllers;

public record CalificacionRequest(double Nota);

[ApiController]
[Route("api/calificaciones")]
[Authorize(Roles = "Coordinador,Administrador")]
public class CalificacionesController : ControllerBase
{
    private readonly SirarContext _db;
    private readonly double _notaMinima;

    public CalificacionesController(SirarContext db, IConfiguration config)
    {
        _db = db;
        _notaMinima = config.GetValue("Reglas:NotaMinimaAprobacion", ReglasAcademicas.NotaMinimaAprobacionPorDefecto);
    }

    [HttpPut("{inscripcionId}")]
    public async Task<IActionResult> ActualizarNota(int inscripcionId, CalificacionRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (!ReglasAcademicas.NotaEnRango(req.Nota))
            return BadRequest(new { mensaje = "La nota debe estar entre 0.0 y 10.0." });

        var inscripcion = await _db.Inscripciones.FindAsync(inscripcionId);
        if (inscripcion is null) return NotFound();

        var notaAnterior = inscripcion.Nota;
        inscripcion.Nota = req.Nota;
        inscripcion.Estado = ReglasAcademicas.EstadoPorNota(req.Nota, _notaMinima);

        _db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            Fecha = DateTime.UtcNow,
            Usuario = User.Identity?.Name ?? "desconocido",
            Accion = "cambio_nota",
            Detalle = $"Inscripcion {inscripcionId}: nota {notaAnterior?.ToString() ?? "sin nota"} -> {req.Nota}"
        });

        await _db.SaveChangesAsync();
        return Ok(inscripcion);
    }
}
