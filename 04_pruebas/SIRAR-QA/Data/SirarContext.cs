using Microsoft.EntityFrameworkCore;
using SirarAuditoria.Models;

namespace SirarAuditoria.Data;

public class SirarContext : DbContext
{
    public SirarContext(DbContextOptions<SirarContext> options) : base(options) { }

    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<Modulo>()
            .HasMany(m => m.Prerrequisitos)
            .WithMany();
    }
}
