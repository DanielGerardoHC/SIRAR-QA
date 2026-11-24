using SirarAuditoria.Models;

namespace SirarAuditoria.Data;

public static class SeedData
{
    public static void Initialize(SirarContext db)
    {
        if (db.Modulos.Any()) return; // ya sembrado

        // ---- Módulos (subconjunto simplificado del plan de estudios) ----
        var bd1 = new Modulo { Codigo = "COIDS0204", Nombre = "Diseño de Bases de Datos", Ciclo = 1 };
        var bd2 = new Modulo { Codigo = "COIDS0704", Nombre = "Arquitectura de Datos Integral", Ciclo = 2 };
        var poo = new Modulo { Codigo = "COIDS1104", Nombre = "Programación Orientada a Objetos", Ciclo = 1 };
        var ed  = new Modulo { Codigo = "COIDS1504", Nombre = "Manejo de Estructuras de Datos", Ciclo = 4 };
        var bdAvz = new Modulo { Codigo = "COIDS1204", Nombre = "Desarrollo de Funciones Avanzadas de Bases de Datos", Ciclo = 3 };

        bd2.Prerrequisitos.Add(bd1);
        ed.Prerrequisitos.Add(poo);
        bdAvz.Prerrequisitos.Add(bd2);

        db.Modulos.AddRange(bd1, bd2, poo, ed, bdAvz);

        // ---- Estudiantes ----
        var normal = new Estudiante { Carnet = "SW-2023-014", Nombre = "Estudiante Regular Uno" };
        var sospechoso = new Estudiante { Carnet = "SW-2023-091", Nombre = "Estudiante Bajo Investigación" }; // nombre ficticio
        db.Estudiantes.AddRange(normal, sospechoso);

        db.SaveChanges();

        // ---- Inscripciones normales ----
        db.Inscripciones.AddRange(
            new Inscripcion { EstudianteId = normal.Id, ModuloId = bd1.Id, Ciclo = "2025-C2", Nota = 7.5, Estado = "aprobado" },
            new Inscripcion { EstudianteId = normal.Id, ModuloId = poo.Id, Ciclo = "2025-C2", Nota = 8.0, Estado = "aprobado" },
            new Inscripcion { EstudianteId = normal.Id, ModuloId = ed.Id, Ciclo = "2026-C1", Nota = null, Estado = "cursando" }
        );

        // ---- Inscripciones del estudiante bajo investigación ----
        // "sospechoso" NUNCA aprobó bd1 (Diseño de Bases de Datos), por lo que
        // bdAvz (que requiere bd2, que a su vez requiere bd1) jamás debería estar disponible.
        db.Inscripciones.AddRange(
            new Inscripcion { EstudianteId = sospechoso.Id, ModuloId = bd1.Id, Ciclo = "2025-C2", Nota = 4.2, Estado = "reprobado" },

            // --- EVIDENCIA DEL INCIDENTE (D1) ---
            // Esta inscripción NO debería existir: bd2 requiere bd1 aprobado, y bd1 está reprobado.
            // Se creó llamando directamente a la API, sin pasar por la validación de la interfaz,
            // porque la regla R1 solo estaba implementada en el frontend (ver MatriculaController).
            new Inscripcion { EstudianteId = sospechoso.Id, ModuloId = bd2.Id, Ciclo = "2026-C1", Nota = null, Estado = "cursando" },

            // --- EVIDENCIA DEL INCIDENTE (D3 + D4) ---
            // La nota de este módulo fue modificada de 4.5 a 8.0 sin que exista ningún
            // registro en RegistrosAuditoria (ver CalificacionesController), porque el
            // endpoint no valida el rol de quien hace el cambio ni registra la acción.
            new Inscripcion { EstudianteId = sospechoso.Id, ModuloId = poo.Id, Ciclo = "2025-C2", Nota = 8.0, Estado = "aprobado" }
        );

        // ---- Usuarios (contraseñas en texto plano: D5) ----
        db.Usuarios.AddRange(
            new Usuario { Email = "coordinador@itca.edu.sv", Password = "Coord2026!", Rol = "Coordinador" },
            new Usuario { Email = "admin@itca.edu.sv", Password = "Admin2026!", Rol = "Administrador" },
            new Usuario { Email = "sw2023091@itca.edu.sv", Password = "estudiante123", Rol = "Estudiante", EstudianteId = sospechoso.Id },
            new Usuario { Email = "sw2023014@itca.edu.sv", Password = "estudiante123", Rol = "Estudiante", EstudianteId = normal.Id }
        );

        // Nota para quien audite: observe que "estudiante123" se repite entre varias cuentas.
        // Esto, sumado a la falta de límite de intentos de login (D6), es relevante para el
        // análisis de riesgos de la Unidad 2.

        db.SaveChanges();
    }
}
