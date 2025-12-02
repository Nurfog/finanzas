using Microsoft.EntityFrameworkCore;
using FinancialAnalytics.API.Data;
using FinancialAnalytics.API.Models;
using FinancialAnalytics.API.Models.Legacy;
using System.Text.Json;
using FinancialAnalytics.API.Models;

namespace FinancialAnalytics.API.Services;

public class LegacyDataSyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LegacyDataSyncService> _logger;
    private const int BATCH_SIZE = 1000;

    public LegacyDataSyncService(IServiceProvider serviceProvider, ILogger<LegacyDataSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SyncDataAsync()
    {
        var status = SyncStatus.Instance;
        
        // Check if already running
        if (status.IsRunning)
        {
            _logger.LogWarning("Sincronización ya en progreso, omitiendo");
            return;
        }
        
        status.StartSync();
        _logger.LogInformation("=== Iniciando sincronización de datos legacy ===");
        var startTime = DateTime.Now;

        using var scope = _serviceProvider.CreateScope();
        var legacyContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
        var financialContext = scope.ServiceProvider.GetRequiredService<FinancialDbContext>();

        // Ensure database exists
        await financialContext.Database.EnsureCreatedAsync();

        try
        {
            status.UpdateProgress("Clientes", 10, "Sincronizando clientes...");
            await SyncCustomers(legacyContext, financialContext);
            
            status.UpdateProgress("Estudiantes", 25, "Sincronizando estudiantes...");
            await SyncStudents(legacyContext, financialContext);
            
            status.UpdateProgress("Sedes y Salas", 45, "Sincronizando sedes y salas...");
            await EnsureLocations(financialContext);
            
            status.UpdateProgress("Transacciones", 65, "Sincronizando transacciones...");
            await SyncTransactions(legacyContext, financialContext);
            
            // Diagnósticos omitidos: vw_legacy_diagnostico no contiene IDs necesarios
            // status.UpdateProgress("Diagnósticos", 85, "Sincronizando diagnósticos...");
            // await SyncDiagnostics(legacyContext, financialContext);

            var duration = DateTime.Now - startTime;
            _logger.LogInformation($"=== Sincronización completada en {duration.TotalSeconds:F2} segundos ===");
            
            status.CompleteSync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fatal durante la sincronización");
            status.CompleteSync(false, ex.Message);
            throw;
        }
    }

    private async Task SyncDiagnostics(LegacyDbContext legacy, FinancialDbContext financial)
    {
        try
        {
            _logger.LogInformation("Sincronizando diagnósticos...");
            var startTime = DateTime.Now;

            // 1. Get all diagnostics and answers
            var diagnostics = await legacy.Diagnostics.ToListAsync();
            var answers = await legacy.DiagnosticAnswers.ToListAsync();
            
            _logger.LogInformation($"Encontrados {diagnostics.Count} diagnósticos y {answers.Count} respuestas en legacy");

            // 2. Map answers by DiagnosticId
            var answersMap = answers
                .GroupBy(a => a.DiagnosticoId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.Respuesta).ToList());

            // 3. Get existing students map (Rut -> Id)
            // Note: We assume Student.Email or another field might map to Rut, but currently Student model doesn't have Rut.
            // The LegacyStudent has Rut. The Student model has Email.
            // We need to link LegacyDiagnostic.IdAlumno (Rut) -> Student.Id.
            // We can do this by: LegacyDiagnostic.IdAlumno -> LegacyStudent.Email -> Student.Email -> Student.Id
            
            var legacyStudents = await legacy.Students.ToDictionaryAsync(s => s.Rut, s => s.Email);
            var financialStudents = await financial.Students.ToDictionaryAsync(s => s.Email.ToLower(), s => s.Id);
            
            var existingDiagnostics = new HashSet<int>(
                await financial.Diagnostics.Select(d => d.Id).ToListAsync()
            );

            var newDiagnostics = new List<DiagnosticResult>();

            foreach (var diag in diagnostics)
            {
                if (existingDiagnostics.Contains(diag.Id)) continue;

                // Link to Student
                if (!legacyStudents.TryGetValue(diag.IdAlumno, out var email)) 
                {
                    _logger.LogWarning($"Omitiendo diagnóstico {diag.Id}: Rut {diag.IdAlumno} no encontrado en legacy students.");
                    continue;
                }

                if (!financialStudents.TryGetValue(email.ToLower(), out var studentId))
                {
                    _logger.LogWarning($"Omitiendo diagnóstico {diag.Id}: Email {email} no encontrado en financial students.");
                    continue;
                }

                // Get answers
                var diagAnswers = answersMap.ContainsKey(diag.Id) ? answersMap[diag.Id] : new List<string>();

                newDiagnostics.Add(new DiagnosticResult
                {
                    Id = diag.Id, // Keep original ID if possible, or let DB generate it. 
                                  // Since we check existingDiagnostics by ID, we should probably set it manually 
                                  // BUT DiagnosticResult.Id might be auto-increment. 
                                  // Let's assume we want to track the legacy ID. 
                                  // Actually, for simplicity, let's just insert and rely on the content.
                                  // Wait, if we use Identity column, we can't force ID easily.
                                  // Let's check if we mapped Id in DiagnosticResult. 
                                  // We didn't specify DatabaseGeneratedOption.None.
                                  // So we should probably NOT set Id and let it auto-increment, 
                                  // BUT then we can't easily check 'existingDiagnostics' by ID unless we store LegacyId.
                                  // For now, let's assume we can insert Identity or just check by (StudentId, Date).
                                  // To be safe and simple: Check by (StudentId, Date) to avoid duplicates if we run this multiple times.
                    StudentId = studentId,
                    AssessmentDate = diag.Fecha,
                    Score = 0, // We don't have a score calculation logic yet, defaulting to 0
                    Type = "Adults",
                    ResultData = JsonSerializer.Serialize(diagAnswers)
                });
            }

            // Filter out duplicates based on StudentId + Date if we can't rely on ID
            // Or better, just insert. The check 'existingDiagnostics.Contains(diag.Id)' implies we wanted to use the ID.
            // If we want to use the Legacy ID as the PK, we need to turn off Identity.
            // For now, I will NOT set the ID and I will change the duplicate check to use StudentId + Date.
            
            var existingKeys = new HashSet<string>(
                await financial.Diagnostics.Select(d => $"{d.StudentId}|{d.AssessmentDate:yyyy-MM-dd}").ToListAsync()
            );

            var finalBatch = new List<DiagnosticResult>();
            foreach (var item in newDiagnostics)
            {
                var key = $"{item.StudentId}|{item.AssessmentDate:yyyy-MM-dd}";
                if (existingKeys.Contains(key)) continue;
                
                existingKeys.Add(key); // Avoid duplicates within the batch
                finalBatch.Add(item);
            }

            if (finalBatch.Any())
            {
                for (int i = 0; i < finalBatch.Count; i += BATCH_SIZE)
                {
                    var batch = finalBatch.Skip(i).Take(BATCH_SIZE).ToList();
                    await financial.Diagnostics.AddRangeAsync(batch);
                    await financial.SaveChangesAsync();
                    _logger.LogInformation($"Diagnósticos: {Math.Min(i + BATCH_SIZE, finalBatch.Count)}/{finalBatch.Count}");
                }
                
                var duration = DateTime.Now - startTime;
                _logger.LogInformation($"✓ Sincronizados {finalBatch.Count} diagnósticos en {duration.TotalSeconds:F2}s");
            }
            else
            {
                _logger.LogInformation("No hay diagnósticos nuevos para sincronizar");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando diagnósticos");
            throw;
        }
    }

    private async Task SyncCustomers(LegacyDbContext legacy, FinancialDbContext financial)
    {
        try
        {
            _logger.LogInformation("Sincronizando clientes...");
            var startTime = DateTime.Now;

            var legacyClients = await legacy.Clients.ToListAsync();
            _logger.LogInformation($"Encontrados {legacyClients.Count} clientes en legacy");

            var existingEmails = new HashSet<string>(
                await financial.Customers.Select(c => c.Email).ToListAsync(),
                StringComparer.OrdinalIgnoreCase
            );
            
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newCustomers = new List<Customer>();

            foreach (var client in legacyClients)
            {
                if (string.IsNullOrWhiteSpace(client.Email)) continue;
                if (existingEmails.Contains(client.Email)) continue;
                if (seenEmails.Contains(client.Email)) continue;

                seenEmails.Add(client.Email);
                newCustomers.Add(new Customer
                {
                    Name = $"{client.Nombres} {client.ApPaterno} {client.ApMaterno}".Trim(),
                    Email = client.Email,
                    Phone = client.Fono ?? "",
                    RegistrationDate = DateTime.Now.AddMonths(-6),
                    CustomerType = "Regular",
                    IsActive = true
                });
            }

            if (newCustomers.Any())
            {
                // Process in batches
                for (int i = 0; i < newCustomers.Count; i += BATCH_SIZE)
                {
                    var batch = newCustomers.Skip(i).Take(BATCH_SIZE).ToList();
                    await financial.Customers.AddRangeAsync(batch);
                    await financial.SaveChangesAsync();
                    _logger.LogInformation($"Clientes: {Math.Min(i + BATCH_SIZE, newCustomers.Count)}/{newCustomers.Count}");
                }

                var duration = DateTime.Now - startTime;
                _logger.LogInformation($"✓ Sincronizados {newCustomers.Count} clientes en {duration.TotalSeconds:F2}s");
            }
            else
            {
                _logger.LogInformation("No hay clientes nuevos para sincronizar");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando clientes");
            throw;
        }
    }

    private async Task SyncStudents(LegacyDbContext legacy, FinancialDbContext financial)
    {
        try
        {
            _logger.LogInformation("Sincronizando estudiantes...");
            var startTime = DateTime.Now;

            var legacyStudents = await legacy.Students.ToListAsync();
            _logger.LogInformation($"Encontrados {legacyStudents.Count} estudiantes en legacy");

            var existingEmails = new HashSet<string>(
                await financial.Students.Select(s => s.Email).ToListAsync(),
                StringComparer.OrdinalIgnoreCase
            );
            
            var customerMap = await financial.Customers.ToDictionaryAsync(c => c.Email.ToLower(), c => c.Id);
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newStudents = new List<Student>();

            foreach (var student in legacyStudents)
            {
                if (string.IsNullOrWhiteSpace(student.Email)) continue;
                if (existingEmails.Contains(student.Email)) continue;
                if (seenEmails.Contains(student.Email)) continue;

                if (!customerMap.TryGetValue(student.Email.ToLower(), out var customerId))
                {
                    _logger.LogWarning($"Omitiendo estudiante {student.Rut}: Email {student.Email} no encontrado en Clientes.");
                    continue;
                }

                seenEmails.Add(student.Email);
                newStudents.Add(new Student
                {
                    Name = $"{student.Nombres} {student.ApPaterno} {student.ApMaterno}".Trim(),
                    Email = student.Email,
                    CustomerId = customerId,
                    EnrollmentDate = DateTime.Now.AddMonths(-3),
                    Program = "General",
                    IsActive = true
                });
            }

            if (newStudents.Any())
            {
                // Process in batches
                for (int i = 0; i < newStudents.Count; i += BATCH_SIZE)
                {
                    var batch = newStudents.Skip(i).Take(BATCH_SIZE).ToList();
                    await financial.Students.AddRangeAsync(batch);
                    await financial.SaveChangesAsync();
                    _logger.LogInformation($"Estudiantes: {Math.Min(i + BATCH_SIZE, newStudents.Count)}/{newStudents.Count}");
                }

                var duration = DateTime.Now - startTime;
                _logger.LogInformation($"✓ Sincronizados {newStudents.Count} estudiantes en {duration.TotalSeconds:F2}s");
            }
            else
            {
                _logger.LogInformation("No hay estudiantes nuevos para sincronizar");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando estudiantes");
            throw;
        }
    }

    private async Task EnsureLocations(FinancialDbContext financial)
    {
        try
        {
            _logger.LogInformation("Sincronizando sedes y salas desde legacy...");
            var startTime = DateTime.Now;

            using var scope = _serviceProvider.CreateScope();
            var legacyContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();

            // Get unique locations from the view
            var courseDetails = await legacyContext.CourseDetails.ToListAsync();
            
            var uniqueLocations = courseDetails
                .GroupBy(cd => cd.Sede)
                .Select(g => g.First())
                .ToList();

            var existingLocationNames = new HashSet<string>(
                await financial.Locations.Select(l => l.Name).ToListAsync(),
                StringComparer.OrdinalIgnoreCase
            );

            var newLocations = new List<Location>();
            foreach (var loc in uniqueLocations)
            {
                if (string.IsNullOrWhiteSpace(loc.Sede)) continue;
                if (existingLocationNames.Contains(loc.Sede)) continue;

                newLocations.Add(new Location
                {
                    Name = loc.Sede,
                    Address = "Dirección pendiente", // Could be enhanced if address is in another table
                    City = "Santiago", // Default, could be enhanced
                    Country = "Chile",
                    OpeningDate = DateTime.Now.AddYears(-2) // Default
                });
            }

            if (newLocations.Any())
            {
                await financial.Locations.AddRangeAsync(newLocations);
                await financial.SaveChangesAsync();
                _logger.LogInformation($"✓ Sincronizadas {newLocations.Count} sedes");
            }

            // Now sync rooms
            var locationMap = await financial.Locations.ToDictionaryAsync(l => l.Name, l => l.Id);
            
            var uniqueRooms = courseDetails
                .Where(cd => !string.IsNullOrWhiteSpace(cd.Sala) && !string.IsNullOrWhiteSpace(cd.Sede))
                .GroupBy(cd => new { cd.Sede, cd.Sala })
                .Select(g => new { 
                    Sede = g.Key.Sede, 
                    Sala = g.Key.Sala, 
                    Capacidad = g.First().Capacidad 
                })
                .ToList();

            var existingRoomKeys = new HashSet<string>(
                await financial.Rooms
                    .Include(r => r.Location)
                    .Select(r => $"{r.Location.Name}|{r.Name}")
                    .ToListAsync(),
                StringComparer.OrdinalIgnoreCase
            );

            var newRooms = new List<Room>();
            foreach (var room in uniqueRooms)
            {
                var key = $"{room.Sede}|{room.Sala}";
                if (existingRoomKeys.Contains(key)) continue;
                if (!locationMap.TryGetValue(room.Sede, out var locationId)) continue;

                newRooms.Add(new Room
                {
                    Name = room.Sala,
                    Capacity = room.Capacidad,
                    RoomType = "Classroom", // Default
                    LocationId = locationId
                });
            }

            if (newRooms.Any())
            {
                await financial.Rooms.AddRangeAsync(newRooms);
                await financial.SaveChangesAsync();
                _logger.LogInformation($"✓ Sincronizadas {newRooms.Count} salas");
            }

            var duration = DateTime.Now - startTime;
            _logger.LogInformation($"✓ Sincronización de sedes y salas completada en {duration.TotalSeconds:F2}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando sedes y salas");
            throw;
        }
    }

    private async Task SyncTransactions(LegacyDbContext legacy, FinancialDbContext financial)
    {
        try
        {
            _logger.LogInformation("Sincronizando transacciones...");
            var startTime = DateTime.Now;

            var legacySales = await legacy.Sales.ToListAsync();
            _logger.LogInformation($"Encontradas {legacySales.Count} ventas en legacy");

            var legacyClients = await legacy.Clients.ToDictionaryAsync(c => c.IdCliente, c => c.Email);
            var customerMap = await financial.Customers.ToDictionaryAsync(c => c.Email.ToLower(), c => c.Id);
            
            // Create location map (Name -> Id)
            var locationMap = await financial.Locations.ToDictionaryAsync(l => l.Name, l => l.Id, StringComparer.OrdinalIgnoreCase);
            
            var newTransactions = new List<Transaction>();

            // Get existing transactions to update or skip
            var existingTransactionsList = await financial.Transactions
                .Where(t => t.Description.StartsWith("Legacy Sale"))
                .ToListAsync();

            var existingTransactions = existingTransactionsList
                .GroupBy(t => t.Description)
                .ToDictionary(g => g.Key, g => g.First());

            var transactionsToAdd = new List<Transaction>();
            var transactionsToUpdate = new List<Transaction>();

            foreach (var sale in legacySales)
            {
                var description = $"Legacy Sale {sale.IdVenta}";
                var paymentMethod = sale.MetodosPago ?? "Efectivo"; // Default to Efectivo if null

                if (existingTransactions.TryGetValue(description, out var existingTransaction))
                {
                    // Update if payment method is different or was "Legacy"
                    if (existingTransaction.PaymentMethod != paymentMethod && existingTransaction.PaymentMethod == "Legacy")
                    {
                        existingTransaction.PaymentMethod = paymentMethod;
                        transactionsToUpdate.Add(existingTransaction);
                    }
                    continue;
                }

                if (!legacyClients.TryGetValue(sale.IdCliente, out var email))
                {
                    _logger.LogWarning($"Omitiendo venta {sale.IdVenta}: Cliente ID {sale.IdCliente} no encontrado en clientes legacy.");
                    continue;
                }

                if (!customerMap.TryGetValue(email.ToLower(), out var customerId))
                {
                    _logger.LogWarning($"Omitiendo venta {sale.IdVenta}: Email de cliente {email} no encontrado en Clientes financieros.");
                    continue;
                }

                int? locationId = null;
                if (!string.IsNullOrEmpty(sale.IdSede) && locationMap.TryGetValue(sale.IdSede, out var locId))
                {
                    locationId = locId;
                }

                transactionsToAdd.Add(new Transaction
                {
                    CustomerId = customerId,
                    LocationId = locationId,
                    TransactionDate = sale.FechaVenta,
                    Amount = sale.Total,
                    TransactionType = "Sale",
                    PaymentMethod = paymentMethod,
                    Status = "Completed",
                    Description = description
                });
            }

            if (transactionsToUpdate.Any())
            {
                _logger.LogInformation($"Actualizando {transactionsToUpdate.Count} transacciones existentes con nuevo método de pago...");
                // Update in batches
                for (int i = 0; i < transactionsToUpdate.Count; i += BATCH_SIZE)
                {
                    var batch = transactionsToUpdate.Skip(i).Take(BATCH_SIZE).ToList();
                    financial.Transactions.UpdateRange(batch);
                    await financial.SaveChangesAsync();
                }
                _logger.LogInformation($"✓ Actualizadas {transactionsToUpdate.Count} transacciones.");
            }

            if (transactionsToAdd.Any())
            {
                // Process in batches
                for (int i = 0; i < transactionsToAdd.Count; i += BATCH_SIZE)
                {
                    var batch = transactionsToAdd.Skip(i).Take(BATCH_SIZE).ToList();
                    await financial.Transactions.AddRangeAsync(batch);
                    await financial.SaveChangesAsync();
                    _logger.LogInformation($"Transacciones nuevas: {Math.Min(i + BATCH_SIZE, transactionsToAdd.Count)}/{transactionsToAdd.Count}");
                }

                var duration = DateTime.Now - startTime;
                _logger.LogInformation($"✓ Sincronizadas {transactionsToAdd.Count} nuevas transacciones en {duration.TotalSeconds:F2}s");
            }
            else
            {
                _logger.LogInformation("No hay transacciones nuevas para sincronizar");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sincronizando transacciones: {ex.InnerException?.Message}");
            throw;
        }
    }
}
