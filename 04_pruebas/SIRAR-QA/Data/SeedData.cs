using SirarAuditoria.Models;
using SirarAuditoria.Security;

namespace SirarAuditoria.Data;

public static class SeedData
{
    public static void Initialize(SirarContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (db.Modulos.Any()) return;

        var bd1 = new Modulo { Codigo = "COIDS0204", Nombre = "Diseño de Bases de Datos", Ciclo = 1 };
        var bd2 = new Modulo { Codigo = "COIDS0704", Nombre = "Arquitectura de Datos Integral", Ciclo = 2 };
        var poo = new Modulo { Codigo = "COIDS1104", Nombre = "Programación Orientada a Objetos", Ciclo = 1 };
        var ed  = new Modulo { Codigo = "COIDS1504", Nombre = "Manejo de Estructuras de Datos", Ciclo = 4 };
        var bdAvz = new Modulo { Codigo = "COIDS1204", Nombre = "Desarrollo de Funciones Avanzadas de Bases de Datos", Ciclo = 3 };

        bd2.Prerrequisitos.Add(bd1);
        ed.Prerrequisitos.Add(poo);
        bdAvz.Prerrequisitos.Add(bd2);

        db.Modulos.AddRange(bd1, bd2, poo, ed, bdAvz);

        var normal = new Estudiante { Carnet = "SW-2023-014", Nombre = "Estudiante Regular Uno" };
        var sospechoso = new Estudiante { Carnet = "SW-2023-091", Nombre = "Estudiante Bajo Investigación" };
        db.Estudiantes.AddRange(normal, sospechoso);

        db.SaveChanges();

        db.Inscripciones.AddRange(
            new Inscripcion { EstudianteId = normal.Id, ModuloId = bd1.Id, Ciclo = "2025-C2", Nota = 7.5, Estado = "aprobado" },
            new Inscripcion { EstudianteId = normal.Id, ModuloId = poo.Id, Ciclo = "2025-C2", Nota = 8.0, Estado = "aprobado" },
            new Inscripcion { EstudianteId = normal.Id, ModuloId = ed.Id, Ciclo = "2026-C1", Nota = null, Estado = "cursando" }
        );

        db.Inscripciones.AddRange(
            new Inscripcion { EstudianteId = sospechoso.Id, ModuloId = bd1.Id, Ciclo = "2025-C2", Nota = 4.2, Estado = "reprobado" },
            new Inscripcion { EstudianteId = sospechoso.Id, ModuloId = bd2.Id, Ciclo = "2026-C1", Nota = null, Estado = "cursando" },
            new Inscripcion { EstudianteId = sospechoso.Id, ModuloId = poo.Id, Ciclo = "2025-C2", Nota = 8.0, Estado = "aprobado" }
        );

        db.Usuarios.AddRange(
            new Usuario { Email = "coordinador@itca.edu.sv", Password = "Coord2026!", Rol = "Coordinador" },
            new Usuario { Email = "admin@itca.edu.sv", Password = "Admin2026!", Rol = "Administrador" },
            new Usuario { Email = "sw2023091@itca.edu.sv", Password = "estudiante123", Rol = "Estudiante", EstudianteId = sospechoso.Id },
            new Usuario { Email = "sw2023014@itca.edu.sv", Password = "estudiante123", Rol = "Estudiante", EstudianteId = normal.Id }
        );

        db.SaveChanges();
    }

    // Hash legacy plain passwords
    public static void HashPlainPasswords(SirarContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        var pendientes = db.Usuarios.AsEnumerable().Where(u => !ContrasenaSegura.EsHash(u.Password)).ToList();
        foreach (var usuario in pendientes)
            usuario.Password = ContrasenaSegura.Hashear(usuario.Password);
        if (pendientes.Count > 0) db.SaveChanges();
    }
}
