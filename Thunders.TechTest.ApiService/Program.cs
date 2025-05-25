using ApiService.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Thunders.TechTest.ApiService;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Dtos.Relatorios.FaturamentoPorHora;
using Thunders.TechTest.ApiService.Dtos.Relatorios.TopPracas;
using Thunders.TechTest.ApiService.Dtos.Relatorios.VeiculosPorPraca;
using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Services;
using Thunders.TechTest.ApiService.Services.Relatorios;
using Thunders.TechTest.OutOfBox.Database;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Fixando portas da api
builder.WebHost.UseUrls("https://localhost:7000", "http://localhost:7001");

var features = Features.BindFromConfiguration(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

builder.AddSqlServerDbContext<AppDbContext>("ThundersTechTestDb", configureDbContextOptions: options =>
{
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Sistema de Relat�rios de Ped�gio",
        Version = "v1",
        Description = "API para processamento de dados de utiliza��es de ped�gio e gera��o de relat�rios"
    });
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Servi�os personalizados
builder.Services.AddScoped<IUtilizacaoService, UtilizacaoService>();
builder.Services.AddScoped<IBulkInsertService, BulkInsertService>();

builder.Services.AddScoped<IValidator<RelatorioFaturamentoPorHoraRequestDto>, RelatorioFaturamentoPorHoraValidator>();
builder.Services.AddScoped<IValidator<RelatorioTopPracasRequestDto>, RelatorioTopPracasValidator>();
builder.Services.AddScoped<IValidator<RelatorioVeiculosPorPracaRequestDto>, RelatorioVeiculosPorPracaValidator>();

builder.Services.AddScoped<IRelatorioService, RelatorioService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Configura��o do Rebus (Message Bus) - COM PORTAS FIXAS
//if (features.UseMessageBroker)
//{
//    // Registra handlers primeiro
//    builder.Services.AddTransient<UtilizacaoMessageHandler>();

//    // Configura Rebus com portas fixas
//    builder.Services.AddRebus(configure => configure
//        .Transport(t => t.UseRabbitMq(
//            connectionString: "amqp://guest:guest@localhost:5672",
//            inputQueueName: "thunders-utilizacoes"
//        ))
//        .Routing(r => r.TypeBased()
//            .Map<UtilizacaoMessage>("thunders-utilizacoes")
//        ));

//    // Auto-registra handlers
//    builder.Services.AutoRegisterHandlersFromAssemblyOf<UtilizacaoMessageHandler>();
//}


builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1)
            }));
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddSource("PedagioSystem");
    });

var app = builder.Build();

await InitializeDatabaseAsync(app);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ped�gio API v1");
        c.RoutePrefix = "";
    });
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    await next();
    stopwatch.Stop();

    if (stopwatch.ElapsedMilliseconds > 5000)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Request lenta: {Method} {Path} - {ElapsedMs}ms",
            context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
    }
});

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();

#region Forcando Migration/Seed
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Inicializando banco de dados...");

        await context.Database.EnsureCreatedAsync();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Aplicando {Count} migra��es...", pendingMigrations.Count());
            await context.Database.MigrateAsync();
        }

        await SeedInitialDataAsync(context, logger);
        logger.LogInformation("Banco de dados inicializado com sucesso");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao inicializar banco de dados");
        throw;
    }
}

static async Task SeedInitialDataAsync(AppDbContext context, ILogger logger)
{
    if (!await context.Pracas.AnyAsync())
    {
        logger.LogInformation("Inserindo dados iniciais...");

        var pracas = new List<Praca>
        {
            new() { Nome = "Pra�a Rodovia Presidente Dutra - KM 225", Cidade = "S�o Jos� dos Campos", Estado = "SP" },
            new() { Nome = "Pra�a Rodovia Anhanguera - KM 45", Cidade = "Jundia�", Estado = "SP" },
            new() { Nome = "Pra�a Rodovia Bandeirantes - KM 72", Cidade = "Campinas", Estado = "SP" },
            new() { Nome = "Pra�a Rodovia Castello Branco - KM 23", Cidade = "Barueri", Estado = "SP" },
            new() { Nome = "Pra�a Rodovia Washington Lu�s - KM 235", Cidade = "S�o Carlos", Estado = "SP" },
            new() { Nome = "Pra�a Rodovia BR-040 - KM 329", Cidade = "Juiz de Fora", Estado = "MG" },
            new() { Nome = "Pra�a Rodovia Fern�o Dias - KM 679", Cidade = "Betim", Estado = "MG" },
            new() { Nome = "Pra�a Rodovia BR-101 - KM 101", Cidade = "Niter�i", Estado = "RJ" },
            new() { Nome = "Pra�a Rodovia BR-116 - KM 143", Cidade = "Nova Igua�u", Estado = "RJ" },
            new() { Nome = "Pra�a Rodovia BR-381 - KM 442", Cidade = "Betim", Estado = "MG" }
        };

        context.Pracas.AddRange(pracas);
        await context.SaveChangesAsync();
        logger.LogInformation("Dados iniciais inseridos: {Count} pra�as", pracas.Count);
    }
    #endregion
}