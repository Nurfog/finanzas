using Microsoft.EntityFrameworkCore;
using FinancialAnalytics.API.Data;
using FinancialAnalytics.API.Services;
using FinancialAnalytics.API.Models.Legacy;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Financial Analytics API", 
        Version = "v1",
        Description = "API para análisis financiero con IA"
    });
});

// Configurar Base de Datos MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FinancialDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Configurar Legacy Database Context (read-only)
builder.Services.AddDbContext<LegacyDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Configurar CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Registrar Servicios de ML
builder.Services.AddSingleton<MLModelService>();
builder.Services.AddHostedService<MLTrainingService>();

// Registrar otros servicios
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<LegacyDataSyncService>();

var app = builder.Build();

// Configurar el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Analytics API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Inicializar base de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<FinancialDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Aplicar migraciones y datos de prueba
        if (app.Environment.IsDevelopment())
        {
            context.Database.EnsureCreated();
            logger.LogInformation("Base de datos inicializada correctamente");
            
            // Sincronizar datos legacy
            var syncService = services.GetRequiredService<LegacyDataSyncService>();
            await syncService.SyncDataAsync();
            logger.LogInformation("Sincronización de datos legacy completada");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al inicializar la base de datos");
    }
}

app.Run();

