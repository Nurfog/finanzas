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
    public int IdSede { get; set; }
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
