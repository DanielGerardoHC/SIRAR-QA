using SirarAuditoria.Security;
using Xunit;

namespace SIRAR.Tests.Unit;

[Trait("Nivel", "Unitaria")]
public class ContrasenaSeguraTests
{
    [Fact(DisplayName = "D05 - El hash no contiene la contraseña original")]
    public void Hashear_NoGuardaTextoPlano()
    {
        var hash = ContrasenaSegura.Hashear("Coord2026!");

        Assert.DoesNotContain("Coord2026!", hash);
        Assert.True(ContrasenaSegura.EsHash(hash));
    }

    [Fact(DisplayName = "D05 - La contraseña correcta se verifica contra su hash")]
    public void Verificar_ContrasenaCorrecta()
    {
        var hash = ContrasenaSegura.Hashear("estudiante123");
        Assert.True(ContrasenaSegura.Verificar("estudiante123", hash));
    }

    [Fact(DisplayName = "D05 - Una contraseña incorrecta no se verifica")]
    public void Verificar_ContrasenaIncorrecta()
    {
        var hash = ContrasenaSegura.Hashear("estudiante123");
        Assert.False(ContrasenaSegura.Verificar("estudiante124", hash));
    }

    [Fact(DisplayName = "D05 - La misma contraseña produce hashes distintos")]
    public void Hashear_UsaSalDistintaCadaVez()
    {
        Assert.NotEqual(ContrasenaSegura.Hashear("estudiante123"), ContrasenaSegura.Hashear("estudiante123"));
    }

    [Fact(DisplayName = "D05 - Un valor en texto plano no se acepta como hash")]
    public void Verificar_RechazaValorEnTextoPlano()
    {
        Assert.False(ContrasenaSegura.EsHash("estudiante123"));
        Assert.False(ContrasenaSegura.Verificar("estudiante123", "estudiante123"));
    }

    [Fact(DisplayName = "D05 - Un hash con formato inválido no se verifica")]
    public void Verificar_RechazaFormatoInvalido()
    {
        Assert.False(ContrasenaSegura.Verificar("x", "pbkdf2$no-numero$AAAA$AAAA"));
        Assert.False(ContrasenaSegura.Verificar("x", "sha1$1000$AAAA$AAAA"));
    }

    [Fact(DisplayName = "D05 - Valores nulos son rechazados")]
    public void ValoresNulos_SonRechazados()
    {
        Assert.Throws<ArgumentNullException>(() => ContrasenaSegura.Verificar("x", null!));
        Assert.Throws<ArgumentNullException>(() => ContrasenaSegura.EsHash(null!));
    }
}
