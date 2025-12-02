using Microsoft.EntityFrameworkCore;

namespace FinancialAnalytics.API.Models.Legacy;

public class LegacyDbContext : DbContext
{
    public LegacyDbContext(DbContextOptions<LegacyDbContext> options)
        : base(options)
    {
    }

    public DbSet<LegacyClient> Clients { get; set; }
    public DbSet<LegacySale> Sales { get; set; }
    public DbSet<LegacyStudent> Students { get; set; }
    public DbSet<LegacyDiagnostic> Diagnostics { get; set; }
    public DbSet<LegacyDiagnosticAnswer> DiagnosticAnswers { get; set; }
    public DbSet<LegacyCourseDetail> CourseDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map to view vw_legacy_clientes (which references sigecaja.cliente)
        modelBuilder.Entity<LegacyClient>(entity =>
        {
            entity.ToTable("vw_legacy_clientes");
            entity.HasKey(e => e.IdCliente);
            entity.Property(e => e.IdCliente).HasColumnName("idCliente");
            entity.Property(e => e.Nombres).HasColumnName("Nombres");
            entity.Property(e => e.ApPaterno).HasColumnName("ApPaterno");
            entity.Property(e => e.ApMaterno).HasColumnName("ApMaterno");
            entity.Property(e => e.Email).HasColumnName("Email");
            entity.Property(e => e.Fono).HasColumnName("Fono");
        });

        // Map to view vw_legacy_ventas (which references sigecaja.ventas)
        modelBuilder.Entity<LegacySale>(entity =>
        {
            entity.ToTable("vw_legacy_ventas");
            entity.HasKey(e => e.IdVenta);
            entity.Property(e => e.IdVenta).HasColumnName("idVenta");
            entity.Property(e => e.IdCliente).HasColumnName("idCliente");
            entity.Property(e => e.Total).HasColumnName("Total");
            entity.Property(e => e.FechaVenta).HasColumnName("FechaVenta");
            entity.Property(e => e.IdSede).HasColumnName("idSede");
            entity.Property(e => e.MetodosPago).HasColumnName("Formas De Pago");
        });

        // Map to view vw_legacy_alumnos (which references sige_sam_v3.alumnos)
        modelBuilder.Entity<LegacyStudent>(entity =>
        {
            entity.ToTable("vw_legacy_alumnos");
            entity.HasKey(e => e.Rut);
            entity.Property(e => e.Rut).HasColumnName("Rut");
            entity.Property(e => e.Nombres).HasColumnName("Nombres");
            entity.Property(e => e.ApPaterno).HasColumnName("AP_Paterno");
            entity.Property(e => e.ApMaterno).HasColumnName("AP_Materno");
            entity.Property(e => e.Email).HasColumnName("Email");
        });

        // Map to sige_sam_v3.diagnostico
        modelBuilder.Entity<LegacyDiagnostic>(entity =>
        {
            entity.ToTable("diagnostico", "sige_sam_v3");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdAlumno).HasColumnName("idAlumno");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
        });

        // Map to sam_diagnostico.respuestaadultos
        modelBuilder.Entity<LegacyDiagnosticAnswer>(entity =>
        {
            entity.ToTable("respuestaadultos", "sam_diagnostico");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiagnosticoId).HasColumnName("diagnosticoID");
            entity.Property(e => e.Respuesta).HasColumnName("respuesta");
        });

        // Map to view vw_legacy_cursos_sedes
        modelBuilder.Entity<LegacyCourseDetail>(entity =>
        {
            entity.ToTable("vw_legacy_cursos_sedes", "financial_analytics");
            entity.HasNoKey(); // Views often don't have a single PK, or we can use IdCursoAbierto if unique per row? 
                               // The view has one row per student/apoderado due to the join. 
                               // So IdCursoAbierto is NOT unique. 
                               // We should probably use HasNoKey or a composite key if we needed one.
            entity.Property(e => e.IdCursoAbierto).HasColumnName("IdCursoAbierto");
            entity.Property(e => e.Sede).HasColumnName("Sede");
            entity.Property(e => e.NombreCurso).HasColumnName("NombreCurso");
            entity.Property(e => e.Sala).HasColumnName("Sala");
            entity.Property(e => e.Capacidad).HasColumnName("Capacidad");
            entity.Property(e => e.AlumnosInscritos).HasColumnName("AlumnosInscritos");
            entity.Property(e => e.CuposDisponibles).HasColumnName("CuposDisponibles");
            entity.Property(e => e.DiasClases).HasColumnName("DiasClases");
            entity.Property(e => e.FechaInicio).HasColumnName("FechaInicio");
            entity.Property(e => e.FechaFin).HasColumnName("FechaFin");
            entity.Property(e => e.HoraInicio).HasColumnName("HoraInicio");
            entity.Property(e => e.HoraFin).HasColumnName("HoraFin");
            entity.Property(e => e.IdAlumno).HasColumnName("idAlumno");
            entity.Property(e => e.NombreAlumno).HasColumnName("Nombre Alumno");
            entity.Property(e => e.NombreApoderado).HasColumnName("Nombre Apoderado");
            entity.Property(e => e.EmailApoderado).HasColumnName("EmailApoderado");
        });
    }
}

public class LegacyClient
{
    public string IdCliente { get; set; }
    public string Nombres { get; set; }
    public string ApPaterno { get; set; }
    public string ApMaterno { get; set; }
    public string Email { get; set; }
    public string Fono { get; set; }
}

public class LegacySale
{
    public int IdVenta { get; set; }
    public string IdCliente { get; set; }
    public int Total { get; set; }
    public DateTime FechaVenta { get; set; }
    public string? IdSede { get; set; }
    public string? MetodosPago { get; set; }
}

public class LegacyStudent
{
    public string Rut { get; set; }
    public string Nombres { get; set; }
    public string ApPaterno { get; set; }
    public string ApMaterno { get; set; }
    public string Email { get; set; }
}

public class LegacyDiagnostic
{
    public int Id { get; set; }
    public string IdAlumno { get; set; }
    public DateTime Fecha { get; set; }
}

public class LegacyDiagnosticAnswer
{
    public int Id { get; set; }
    public int DiagnosticoId { get; set; }
    public string Respuesta { get; set; }
}

public class LegacyCourseDetail
{
    public int IdCursoAbierto { get; set; }
    public string? Sede { get; set; }
    public string? NombreCurso { get; set; }
    public string? Sala { get; set; }
    public int Capacidad { get; set; }
    public int AlumnosInscritos { get; set; }
    public int CuposDisponibles { get; set; }
    public string? DiasClases { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public string? IdAlumno { get; set; }
    public string? NombreAlumno { get; set; }
    public string? NombreApoderado { get; set; }
    public string? EmailApoderado { get; set; }
}
