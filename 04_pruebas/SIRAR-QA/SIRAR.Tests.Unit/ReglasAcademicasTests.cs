using SirarAuditoria.Models;
using SirarAuditoria.Services;
using Xunit;

namespace SIRAR.Tests.Unit;

[Trait("Nivel", "Unitaria")]
public class ReglasAcademicasTests
{
    [Theory(DisplayName = "D10 R2 - El estado depende de la nota mínima de 7.0")]
    [InlineData(7.0, "aprobado")]
    [InlineData(9.5, "aprobado")]
    [InlineData(6.9, "reprobado")]
    [InlineData(6.5, "reprobado")]
    [InlineData(6.0, "reprobado")]
    [InlineData(4.5, "reprobado")]
    public void EstadoPorNota_UsaNotaMinimaDeAprobacion(double nota, string esperado)
    {
        Assert.Equal(esperado, ReglasAcademicas.EstadoPorNota(nota, ReglasAcademicas.NotaMinimaAprobacionPorDefecto));
    }

    [Fact(DisplayName = "D10 R2 - La nota mínima es configurable")]
    public void EstadoPorNota_RespetaNotaMinimaConfigurada()
    {
        Assert.Equal("aprobado", ReglasAcademicas.EstadoPorNota(6.0, 6.0));
        Assert.Equal("reprobado", ReglasAcademicas.EstadoPorNota(7.5, 8.0));
    }

    [Theory(DisplayName = "D11 - Solo se aceptan notas entre 0.0 y 10.0")]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(8.0, true)]
    [InlineData(-0.1, false)]
    [InlineData(10.1, false)]
    [InlineData(double.NaN, false)]
    public void NotaEnRango_ValidaLimites(double nota, bool esperado)
    {
        Assert.Equal(esperado, ReglasAcademicas.NotaEnRango(nota));
    }

    [Fact(DisplayName = "D01 R1 - Un prerrequisito no aprobado queda pendiente")]
    public void PrerrequisitosPendientes_DetectaPrerrequisitoNoAprobado()
    {
        var bd1 = new Modulo { Id = 1, Codigo = "COIDS0204" };
        var bd2 = new Modulo { Id = 2, Codigo = "COIDS0704", Prerrequisitos = { bd1 } };

        var pendientes = ReglasAcademicas.PrerrequisitosPendientes(bd2, new[] { 3 });

        Assert.Single(pendientes);
        Assert.Equal("COIDS0204", pendientes[0].Codigo);
    }

    [Fact(DisplayName = "D01 R1 - Con el prerrequisito aprobado no hay pendientes")]
    public void PrerrequisitosPendientes_SinPendientesCuandoEstaAprobado()
    {
        var bd1 = new Modulo { Id = 1, Codigo = "COIDS0204" };
        var bd2 = new Modulo { Id = 2, Codigo = "COIDS0704", Prerrequisitos = { bd1 } };

        Assert.Empty(ReglasAcademicas.PrerrequisitosPendientes(bd2, new[] { 1 }));
    }

    [Fact(DisplayName = "D01 R1 - Un módulo con varios prerrequisitos exige todos")]
    public void PrerrequisitosPendientes_ExigeTodosLosPrerrequisitos()
    {
        var a = new Modulo { Id = 1, Codigo = "A" };
        var b = new Modulo { Id = 2, Codigo = "B" };
        var c = new Modulo { Id = 3, Codigo = "C", Prerrequisitos = { a, b } };

        var pendientes = ReglasAcademicas.PrerrequisitosPendientes(c, new[] { 1 });

        Assert.Single(pendientes);
        Assert.Equal("B", pendientes[0].Codigo);
    }

    [Fact(DisplayName = "D01 R1 - Un módulo sin prerrequisitos no tiene pendientes")]
    public void PrerrequisitosPendientes_ModuloSinPrerrequisitos()
    {
        Assert.Empty(ReglasAcademicas.PrerrequisitosPendientes(new Modulo { Id = 9 }, Array.Empty<int>()));
    }

    [Fact(DisplayName = "D01 R1 - Datos nulos son rechazados")]
    public void PrerrequisitosPendientes_RechazaNulos()
    {
        Assert.Throws<ArgumentNullException>(() => ReglasAcademicas.PrerrequisitosPendientes(null!, Array.Empty<int>()));
        Assert.Throws<ArgumentNullException>(() => ReglasAcademicas.PrerrequisitosPendientes(new Modulo(), null!));
    }
}
