namespace SirarAuditoria.Security;

public record JwtSettings(string Secret)
{
    public const string Issuer = "sirar-auditoria";
    public const string Audience = "sirar-auditoria";
}
