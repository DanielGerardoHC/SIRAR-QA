using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SirarAuditoria.Data;
using SirarAuditoria.Models;

namespace SirarAuditoria.Controllers;

public record CalificacionRequest(double Nota);

[ApiController]
[Route("api/calificaciones")]
[Authorize(Roles = "Coordinador,Administrador")]
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

        _db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            Fecha = DateTime.UtcNow,
            Usuario = User.Identity?.Name ?? "desconocido",
            Accion = "cambio_nota",
            Detalle = $"Inscripcion {inscripcionId}: nota -> {req.Nota}"
        });

        await _db.SaveChangesAsync();
        return Ok(inscripcion);
    }
}
