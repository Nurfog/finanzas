using Microsoft.EntityFrameworkCore;
using FinancialAnalytics.API.Models;

namespace FinancialAnalytics.API.Data;

public class FinancialDbContext : DbContext
{
    public FinancialDbContext(DbContextOptions<FinancialDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomUsage> RoomUsages { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<DiagnosticResult> Diagnostics { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<StudentProgress> StudentProgress { get; set; }
    public DbSet<Report> Reports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Location configuration
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
        });

        // Room configuration
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Location)
                .WithMany(l => l.Rooms)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RoomUsage configuration
        modelBuilder.Entity<RoomUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Room)
                .WithMany(r => r.RoomUsages)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.UtilizationRate).HasPrecision(5, 2);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Location)
                .WithMany(l => l.Transactions)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TransactionDate);
        });

        // Student configuration
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Students)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StudentProgress configuration
        modelBuilder.Entity<StudentProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Score).HasPrecision(5, 2);
            entity.HasOne(e => e.Student)
                .WithMany(s => s.ProgressRecords)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.AssessmentDate);
        });

        // Report configuration
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Content).HasColumnType("LONGTEXT");
        });

        // Seed data disabled - using legacy data sync instead
        // SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Locations
        modelBuilder.Entity<Location>().HasData(
            new Location { Id = 1, Name = "Sede Central", Address = "Av. Principal 123", City = "Santiago", Country = "Chile", OpeningDate = new DateTime(2020, 1, 15) },
            new Location { Id = 2, Name = "Sede Norte", Address = "Calle Norte 456", City = "Santiago", Country = "Chile", OpeningDate = new DateTime(2021, 3, 10) },
            new Location { Id = 3, Name = "Sede Sur", Address = "Av. Sur 789", City = "Valparaíso", Country = "Chile", OpeningDate = new DateTime(2022, 6, 1) }
        );

        // Seed Rooms
        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Name = "Sala A1", Capacity = 30, RoomType = "Classroom", LocationId = 1 },
            new Room { Id = 2, Name = "Sala A2", Capacity = 25, RoomType = "Classroom", LocationId = 1 },
            new Room { Id = 3, Name = "Lab 1", Capacity = 20, RoomType = "Lab", LocationId = 1 },
            new Room { Id = 4, Name = "Sala B1", Capacity = 35, RoomType = "Classroom", LocationId = 2 },
            new Room { Id = 5, Name = "Sala B2", Capacity = 30, RoomType = "Classroom", LocationId = 2 },
            new Room { Id = 6, Name = "Sala C1", Capacity = 40, RoomType = "Conference", LocationId = 3 }
        );

        // Seed Customers
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Name = "Juan Pérez", Email = "juan.perez@example.com", Phone = "+56912345678", RegistrationDate = new DateTime(2023, 1, 10), CustomerType = "Premium" },
            new Customer { Id = 2, Name = "María González", Email = "maria.gonzalez@example.com", Phone = "+56987654321", RegistrationDate = new DateTime(2023, 2, 15), CustomerType = "Regular" },
            new Customer { Id = 3, Name = "Carlos Rodríguez", Email = "carlos.rodriguez@example.com", Phone = "+56911223344", RegistrationDate = new DateTime(2023, 3, 20), CustomerType = "VIP" },
            new Customer { Id = 4, Name = "Ana Martínez", Email = "ana.martinez@example.com", Phone = "+56922334455", RegistrationDate = new DateTime(2023, 4, 5), CustomerType = "Regular" },
            new Customer { Id = 5, Name = "Pedro Silva", Email = "pedro.silva@example.com", Phone = "+56933445566", RegistrationDate = new DateTime(2023, 5, 12), CustomerType = "Premium" }
        );

        // Seed Students
        modelBuilder.Entity<Student>().HasData(
            new Student { Id = 1, Name = "Sofía Pérez", Email = "sofia.perez@example.com", CustomerId = 1, EnrollmentDate = new DateTime(2023, 1, 15), Program = "Matemáticas Avanzadas" },
            new Student { Id = 2, Name = "Diego González", Email = "diego.gonzalez@example.com", CustomerId = 2, EnrollmentDate = new DateTime(2023, 2, 20), Program = "Programación" },
            new Student { Id = 3, Name = "Valentina Rodríguez", Email = "valentina.rodriguez@example.com", CustomerId = 3, EnrollmentDate = new DateTime(2023, 3, 25), Program = "Inglés" },
            new Student { Id = 4, Name = "Mateo Martínez", Email = "mateo.martinez@example.com", CustomerId = 4, EnrollmentDate = new DateTime(2023, 4, 10), Program = "Ciencias" },
            new Student { Id = 5, Name = "Isabella Silva", Email = "isabella.silva@example.com", CustomerId = 5, EnrollmentDate = new DateTime(2023, 5, 18), Program = "Arte" }
        );

        // Seed Transactions (last 6 months)
        var transactions = new List<Transaction>();
        var random = new Random(42); // Fixed seed for reproducible data
        int transactionId = 1;

        for (int month = 0; month < 6; month++)
        {
            var date = DateTime.Now.AddMonths(-month);
            for (int i = 0; i < 20; i++)
            {
                transactions.Add(new Transaction
                {
                    Id = transactionId++,
                    CustomerId = (i % 5) + 1,
                    LocationId = (i % 3) + 1,
                    TransactionDate = date.AddDays(-random.Next(0, 28)),
                    Amount = random.Next(50, 500),
                    TransactionType = i % 4 == 0 ? "Fee" : "Payment",
                    PaymentMethod = i % 3 == 0 ? "Cash" : (i % 3 == 1 ? "Card" : "Transfer"),
                    Description = $"Monthly payment - {date:MMMM yyyy}",
                    Status = "Completed"
                });
            }
        }

        modelBuilder.Entity<Transaction>().HasData(transactions);

        // Seed Room Usage (last 3 months)
        var roomUsages = new List<RoomUsage>();
        int usageId = 1;

        for (int month = 0; month < 3; month++)
        {
            var baseDate = DateTime.Now.AddMonths(-month);
            for (int day = 0; day < 20; day++)
            {
                var currentDate = baseDate.AddDays(-day);
                for (int roomId = 1; roomId <= 6; roomId++)
                {
                    // Morning session
                    roomUsages.Add(new RoomUsage
                    {
                        Id = usageId++,
                        RoomId = roomId,
                        StartTime = currentDate.Date.AddHours(9),
                        EndTime = currentDate.Date.AddHours(12),
                        AttendeeCount = random.Next(10, 35),
                        Purpose = "Class",
                        UtilizationRate = random.Next(60, 95)
                    });

                    // Afternoon session (not every day)
                    if (random.Next(0, 2) == 0)
                    {
                        roomUsages.Add(new RoomUsage
                        {
                            Id = usageId++,
                            RoomId = roomId,
                            StartTime = currentDate.Date.AddHours(14),
                            EndTime = currentDate.Date.AddHours(17),
                            AttendeeCount = random.Next(8, 30),
                            Purpose = random.Next(0, 3) == 0 ? "Meeting" : "Class",
                            UtilizationRate = random.Next(50, 90)
                        });
                    }
                }
            }
        }

        modelBuilder.Entity<RoomUsage>().HasData(roomUsages);

        // Seed Student Progress (last 6 months, monthly assessments)
        var progressRecords = new List<StudentProgress>();
        int progressId = 1;
        var subjects = new[] { "Matemáticas", "Programación", "Inglés", "Ciencias", "Arte" };

        for (int studentId = 1; studentId <= 5; studentId++)
        {
            for (int month = 0; month < 6; month++)
            {
                var assessmentDate = DateTime.Now.AddMonths(-month).AddDays(-random.Next(0, 28));
                var baseScore = 60 + random.Next(0, 30); // Base score between 60-90
                var trend = month * 2; // Slight improvement over time
                var score = Math.Min(100, baseScore + trend + random.Next(-5, 10));

                string performanceLevel;
                if (score >= 85) performanceLevel = "Excellent";
                else if (score >= 70) performanceLevel = "Good";
                else if (score >= 60) performanceLevel = "Average";
                else performanceLevel = "Poor";

                progressRecords.Add(new StudentProgress
                {
                    Id = progressId++,
                    StudentId = studentId,
                    AssessmentDate = assessmentDate,
                    Subject = subjects[studentId - 1],
                    Score = score,
                    AttendanceRate = random.Next(75, 100),
                    PerformanceLevel = performanceLevel,
                    Notes = $"Assessment for {assessmentDate:MMMM yyyy}"
                });
            }
        }

        modelBuilder.Entity<StudentProgress>().HasData(progressRecords);
    }
}
