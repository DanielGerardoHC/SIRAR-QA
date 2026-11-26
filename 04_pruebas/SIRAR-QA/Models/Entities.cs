namespace SirarAuditoria.Models;

public class Modulo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Ciclo { get; set; }

    public List<Modulo> Prerrequisitos { get; set; } = new();
}

public class Estudiante
{
    public int Id { get; set; }
    public string Carnet { get; set; } = "";
    public string Nombre { get; set; } = "";
    public List<Inscripcion> Inscripciones { get; set; } = new();
}

public class Inscripcion
{
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public Estudiante? Estudiante { get; set; }
    public int ModuloId { get; set; }
    public Modulo? Modulo { get; set; }
    public string Ciclo { get; set; } = "";
    public double? Nota { get; set; }
    public string Estado { get; set; } = "cursando"; // cursando | aprobado | reprobado
}

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Rol { get; set; } = "Estudiante"; // Estudiante | Coordinador | Administrador
    public int? EstudianteId { get; set; }
}

public class RegistroAuditoria
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Detalle { get; set; } = "";
}
