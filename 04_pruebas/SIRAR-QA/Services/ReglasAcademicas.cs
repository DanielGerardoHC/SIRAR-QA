using SirarAuditoria.Models;

namespace SirarAuditoria.Services;

public static class ReglasAcademicas
{
    public const double NotaMinimaPosible = 0.0;
    public const double NotaMaximaPosible = 10.0;
    public const double NotaMinimaAprobacionPorDefecto = 7.0;

    public static bool NotaEnRango(double nota) =>
        !double.IsNaN(nota) && nota >= NotaMinimaPosible && nota <= NotaMaximaPosible;

    public static string EstadoPorNota(double nota, double notaMinimaAprobacion) =>
        nota >= notaMinimaAprobacion ? "aprobado" : "reprobado";

    public static IReadOnlyList<Modulo> PrerrequisitosPendientes(Modulo modulo, IEnumerable<int> modulosAprobados)
    {
        ArgumentNullException.ThrowIfNull(modulo);
        ArgumentNullException.ThrowIfNull(modulosAprobados);
        var aprobados = new HashSet<int>(modulosAprobados);
        return modulo.Prerrequisitos.Where(p => !aprobados.Contains(p.Id)).ToList();
    }
}
