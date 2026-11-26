using System.Security.Cryptography;

namespace SirarAuditoria.Security;

public static class ContrasenaSegura
{
    private const string Prefijo = "pbkdf2";
    private const int Iteraciones = 100_000;
    private const int LongitudSal = 16;
    private const int LongitudHash = 32;

    public static string Hashear(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(LongitudSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, LongitudHash);
        return $"{Prefijo}${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string contrasena, string almacenada)
    {
        ArgumentNullException.ThrowIfNull(almacenada);
        var partes = almacenada.Split('$');
        if (partes.Length != 4 || partes[0] != Prefijo || !int.TryParse(partes[1], out var iteraciones))
            return false;

        var sal = Convert.FromBase64String(partes[2]);
        var esperado = Convert.FromBase64String(partes[3]);
        var calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    public static bool EsHash(string valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return valor.StartsWith(Prefijo + "$", StringComparison.Ordinal);
    }
}
