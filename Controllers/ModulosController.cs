using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SirarAuditoria.Data;

namespace SirarAuditoria.Controllers;

[ApiController]
[Route("api/modulos")]
public class ModulosController : ControllerBase
{
    private readonly SirarContext _db;
    public ModulosController(SirarContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var modulos = await _db.Modulos
            .Select(m => new { m.Id, m.Codigo, m.Nombre, m.Ciclo })
            .ToListAsync();
        return Ok(modulos);
    }

    [HttpGet("{codigo}/prerrequisitos")]
    public async Task<IActionResult> Prerrequisitos(string codigo)
    {
        var modulo = await _db.Modulos
            .Include(m => m.Prerrequisitos)
            .FirstOrDefaultAsync(m => m.Codigo == codigo);

        if (modulo is null) return NotFound();

        return Ok(modulo.Prerrequisitos.Select(p => new { p.Codigo, p.Nombre }));
    }
}
