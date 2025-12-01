using FinancialAnalytics.API.Data;
using FinancialAnalytics.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialAnalytics.API.Services;

public class DataSeedingService
{
    private readonly FinancialDbContext _context;
    private readonly ILogger<DataSeedingService> _logger;
    private readonly Random _random = new Random();

    public DataSeedingService(FinancialDbContext context, ILogger<DataSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding check...");

            // Check if we have enough data
            var transactionCount = await _context.Transactions.CountAsync();
            if (transactionCount > 1000)
            {
                _logger.LogInformation($"Database already has robust data ({transactionCount} transactions). Skipping seeding.");
                return;
            }

            if (transactionCount > 0)
            {
                _logger.LogInformation("Found insufficient data. Clearing database to re-seed...");
                // Delete in reverse order of dependencies
                _context.Transactions.RemoveRange(_context.Transactions);
                _context.RoomUsages.RemoveRange(_context.RoomUsages);
                _context.StudentProgress.RemoveRange(_context.StudentProgress);
                _context.Students.RemoveRange(_context.Students);
                _context.Customers.RemoveRange(_context.Customers);
                _context.Rooms.RemoveRange(_context.Rooms);
                _context.Locations.RemoveRange(_context.Locations);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Seeding new data...");

            // Seed Locations
            var locations = await SeedLocations();
            
            // Seed Rooms
            var rooms = await SeedRooms(locations);
            
            // Seed Customers
            var customers = await SeedCustomers();
            
            // Seed Students
            var students = await SeedStudents(customers);
            
            // Seed Transactions (12 months of data)
            await SeedTransactions(customers, locations);
            
            // Seed Room Usage (6 months of data)
            await SeedRoomUsage(rooms);
            
            // Seed Student Progress (multiple assessments per student)
            await SeedStudentProgress(students);

            _logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding database");
            throw;
        }
    }

    private async Task<List<Location>> SeedLocations()
    {
        var locations = new List<Location>
        {
            new Location { Name = "Sede Central", Address = "Av. Principal 123", City = "Santiago", Country = "Chile", OpeningDate = DateTime.Now.AddYears(-3) },
            new Location { Name = "Sede Norte", Address = "Calle Norte 456", City = "Santiago", Country = "Chile", OpeningDate = DateTime.Now.AddYears(-2) },
            new Location { Name = "Sede Sur", Address = "Av. Sur 789", City = "Santiago", Country = "Chile", OpeningDate = DateTime.Now.AddYears(-1) },
            new Location { Name = "Sede Oriente", Address = "Calle Oriente 321", City = "Santiago", Country = "Chile", OpeningDate = DateTime.Now.AddMonths(-6) },
            new Location { Name = "Sede Poniente", Address = "Av. Poniente 654", City = "Santiago", Country = "Chile", OpeningDate = DateTime.Now.AddMonths(-3) }
        };

        _context.Locations.AddRange(locations);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {locations.Count} locations");
        return locations;
    }

    private async Task<List<Room>> SeedRooms(List<Location> locations)
    {
        var rooms = new List<Room>();
        var roomTypes = new[] { "Sala", "Lab", "Auditorio", "Taller" };

        foreach (var location in locations)
        {
            int roomCount = _random.Next(4, 8);
            for (int i = 1; i <= roomCount; i++)
            {
                rooms.Add(new Room
                {
                    Name = $"{roomTypes[_random.Next(roomTypes.Length)]} {location.Name.Split(' ')[1][0]}{i}",
                    LocationId = location.Id,
                    Capacity = _random.Next(15, 50),
                    RoomType = roomTypes[_random.Next(roomTypes.Length)]
                });
            }
        }

        _context.Rooms.AddRange(rooms);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {rooms.Count} rooms");
        return rooms;
    }

    private async Task<List<Customer>> SeedCustomers()
    {
        var firstNames = new[] { "Juan", "María", "Carlos", "Ana", "Pedro", "Sofía", "Diego", "Valentina", "Mateo", "Isabella", 
                                 "Sebastián", "Camila", "Nicolás", "Martina", "Felipe", "Catalina", "Andrés", "Gabriela", "Lucas", "Daniela" };
        var lastNames = new[] { "González", "Rodríguez", "Pérez", "Martínez", "García", "López", "Hernández", "Silva", "Muñoz", "Rojas",
                               "Torres", "Flores", "Rivera", "Gómez", "Díaz", "Vargas", "Castro", "Morales", "Soto", "Reyes" };

        var customers = new List<Customer>();
        for (int i = 0; i < 100; i++)
        {
            var firstName = firstNames[_random.Next(firstNames.Length)];
            var lastName = lastNames[_random.Next(lastNames.Length)];
            
            customers.Add(new Customer
            {
                Name = $"{firstName} {lastName}",
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}@email.com",
                Phone = $"+569{_random.Next(10000000, 99999999)}",
                RegistrationDate = DateTime.Now.AddDays(-_random.Next(180, 730)),
                CustomerType = new[] { "Premium", "Regular", "VIP" }[_random.Next(3)]
            });
        }

        _context.Customers.AddRange(customers);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {customers.Count} customers");
        return customers;
    }

    private async Task<List<Student>> SeedStudents(List<Customer> customers)
    {
        var firstNames = new[] { "Sofía", "Diego", "Valentina", "Mateo", "Isabella", "Sebastián", "Camila", "Nicolás", "Martina", "Felipe",
                                 "Catalina", "Andrés", "Gabriela", "Lucas", "Daniela", "Tomás", "Fernanda", "Benjamín", "Javiera", "Agustín" };
        var lastNames = new[] { "Pérez", "González", "Rodríguez", "Martínez", "Silva", "López", "Hernández", "Torres", "Muñoz", "Rojas" };
        var programs = new[] { "Matemáticas Avanzadas", "Programación", "Inglés", "Ciencias", "Arte", "Música", "Robótica", "Literatura" };

        var students = new List<Student>();
        for (int i = 0; i < 5000; i++)
        {
            var firstName = firstNames[_random.Next(firstNames.Length)];
            var lastName = lastNames[_random.Next(lastNames.Length)];
            
            students.Add(new Student
            {
                Name = $"{firstName} {lastName}",
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}@estudiante.com",
                EnrollmentDate = DateTime.Now.AddDays(-_random.Next(30, 365)),
                Program = programs[_random.Next(programs.Length)],
                Status = new[] { "Active", "Active", "Active", "Graduated", "Suspended" }[_random.Next(5)],
                CustomerId = customers[_random.Next(customers.Count)].Id
            });
        }

        _context.Students.AddRange(students);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {students.Count} students");
        return students;
    }

    private async Task SeedTransactions(List<Customer> customers, List<Location> locations)
    {
        var transactions = new List<Transaction>();
        var paymentMethods = new[] { "Cash", "Card", "Transfer", "MercadoPago" };
        var descriptions = new[] { "Matrícula", "Mensualidad", "Material", "Taller", "Certificación", "Curso Especial" };

        // Generate 12 months of data for better ML training
        var startDate = DateTime.Now.AddMonths(-12);
        var endDate = DateTime.Now;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Base daily transactions with growth trend
            // Start with ~30 and grow to ~80 over the year
            double progress = (date - startDate).TotalDays / (endDate - startDate).TotalDays;
            int baseTransactions = 30 + (int)(50 * progress);
            
            // Add seasonality (more in March/April and Nov/Dec)
            int month = date.Month;
            double seasonalityFactor = 1.0;
            if (month == 3 || month == 4 || month == 11 || month == 12) seasonalityFactor = 1.3;
            if (month == 1 || month == 2) seasonalityFactor = 0.7; // Summer break

            int dailyTransactions = (int)(baseTransactions * seasonalityFactor);
            // Add some random noise
            dailyTransactions += _random.Next(-5, 6);
            
            for (int i = 0; i < dailyTransactions; i++)
            {
                transactions.Add(new Transaction
                {
                    CustomerId = customers[_random.Next(customers.Count)].Id,
                    LocationId = locations[_random.Next(locations.Count)].Id,
                    Amount = _random.Next(50, 500) * 1000, 
                    TransactionDate = date.AddHours(_random.Next(8, 20)).AddMinutes(_random.Next(0, 60)),
                    PaymentMethod = paymentMethods[_random.Next(paymentMethods.Length)],
                    Description = descriptions[_random.Next(descriptions.Length)],
                    Status = "Completed"
                });
            }
        }

        // Insert in batches to avoid memory issues
        var batches = transactions.Chunk(1000);
        foreach (var batch in batches)
        {
            _context.Transactions.AddRange(batch);
            await _context.SaveChangesAsync();
        }
        
        _logger.LogInformation($"Seeded {transactions.Count} transactions with trends and seasonality");
    }

    private async Task SeedRoomUsage(List<Room> rooms)
    {
        var usages = new List<RoomUsage>();
        var startDate = DateTime.Now.AddMonths(-6);
        var endDate = DateTime.Now;

        foreach (var room in rooms)
        {
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Skip some days randomly (weekends, holidays)
                if (_random.Next(100) < 15) continue;

                // 3-8 sessions per room per day for more data
                int sessionsPerDay = _random.Next(3, 9);
                
                for (int i = 0; i < sessionsPerDay; i++)
                {
                    var startHour = 8 + (i * 2);
                    var sessionStart = date.AddHours(startHour);
                    var sessionEnd = sessionStart.AddHours(_random.Next(1, 4));
                    
                    usages.Add(new RoomUsage
                    {
                        RoomId = room.Id,
                        StartTime = sessionStart,
                        EndTime = sessionEnd,
                        Purpose = new[] { "Clase", "Taller", "Reunión", "Examen", "Conferencia" }[_random.Next(5)],
                        AttendeeCount = _random.Next(5, room.Capacity),
                        UtilizationRate = _random.Next(0, 100)
                    });
                }
            }
        }

        _context.RoomUsages.AddRange(usages);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {usages.Count} room usage records");
    }

    private async Task SeedStudentProgress(List<Student> students)
    {
        var progressRecords = new List<StudentProgress>();
        var assessmentTypes = new[] { "Quiz", "Examen", "Proyecto", "Tarea", "Presentación" };

        foreach (var student in students)
        {
            // 15-25 assessments per student over the past 6 months
            int assessmentCount = _random.Next(15, 26);
            var startDate = student.EnrollmentDate;
            
            for (int i = 0; i < assessmentCount; i++)
            {
                var assessmentDate = startDate.AddDays(_random.Next(0, 180));
                
                // Generate scores with some variance but trending based on student ability
                var baseScore = _random.Next(60, 95);
                var variance = _random.Next(-10, 15);
                var score = Math.Max(50, Math.Min(100, baseScore + variance));
                
                var performance = score >= 90 ? "Excellent" : 
                                 score >= 80 ? "Good" : 
                                 score >= 70 ? "Average" : "NeedsImprovement";

                progressRecords.Add(new StudentProgress
                {
                    StudentId = student.Id,
                    AssessmentDate = assessmentDate,
                    Subject = student.Program,
                    AssessmentType = assessmentTypes[_random.Next(assessmentTypes.Length)],
                    Score = score,
                    AttendanceRate = _random.Next(75, 100),
                    PerformanceLevel = performance,
                    Notes = $"Evaluación {i + 1} - {performance}"
                });
            }
        }

        _context.StudentProgress.AddRange(progressRecords);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {progressRecords.Count} student progress records");
    }
}
