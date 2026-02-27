using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SirarAuditoria.Data;

namespace SirarAuditoria.Controllers;

public record CalificacionRequest(double Nota);

[ApiController]
[Route("api/calificaciones")]
[Authorize]
public class CalificacionesController : ControllerBase
{
    private readonly SirarContext _db;
    public CalificacionesController(SirarContext db) => _db = db;

    [HttpPut("{inscripcionId}")]
    public async Task<IActionResult> ActualizarNota(int inscripcionId, CalificacionRequest req)
    {
        var inscripcion = await _db.Inscripciones.FindAsync(inscripcionId);
        if (inscripcion is null) return NotFound();

        inscripcion.Nota = req.Nota;
        inscripcion.Estado = req.Nota >= 6.0 ? "aprobado" : "reprobado";

        await _db.SaveChangesAsync();
        return Ok(inscripcion);
    }
}
