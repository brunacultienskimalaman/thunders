using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Dtos;
using Thunders.TechTest.ApiService.Models.Enums;
using Thunders.TechTest.ApiService.Services;

namespace Thunders.TechTest.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(AppDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                // Verifica se há migrações pendentes
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();

                // Tenta contar praças
                var pracasCount = await _context.Pracas.CountAsync();

                var connectionString = _context.Database.GetConnectionString();

                return Ok(new
                {
                    Status = "Success",
                    DatabaseConnected = true,
                    PracasCount = pracasCount,
                    PendingMigrationsCount = pendingMigrations.Count(),
                    AppliedMigrationsCount = appliedMigrations.Count(),
                    ConnectionString = connectionString?.Substring(0, Math.Min(50, connectionString.Length)) + "...", // Apenas início da connection string por segurança
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");

                return Ok(new
                {
                    Status = "Error",
                    DatabaseConnected = false,
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message,
                    Timestamp = DateTime.Now
                });
            }
        }

        [HttpPost("migrate")]
        public async Task<IActionResult> RunMigrations()
        {
            try
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();

                if (!pendingMigrations.Any())
                {
                    return Ok(new { Message = "No pending migrations", Status = "Success" });
                }

                await _context.Database.MigrateAsync();

                return Ok(new
                {
                    Message = $"Applied {pendingMigrations.Count()} migrations successfully",
                    AppliedMigrations = pendingMigrations.ToList(),
                    Status = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed");

                return BadRequest(new
                {
                    Status = "Error",
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message
                });
            }
        }


        [HttpGet("tables")]
        public async Task<IActionResult> CheckTables()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();

                // Lista todas as tabelas
                var tables = await _context.Database.SqlQueryRaw<string>(
                    "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
                ).ToListAsync();

                var pracasCount = await _context.Pracas.CountAsync();

                return Ok(new
                {
                    CanConnect = canConnect,
                    Tables = tables,
                    PracasCount = pracasCount,
                    DatabaseName = _context.Database.GetDbConnection().Database
                });
            }
            catch (Exception ex)
            {
                return Ok(new { Error = ex.Message });
            }
        }

        [HttpGet("pracas")]
        public async Task<IActionResult> GetPracas()
        {
            try
            {
                var pracas = await _context.Pracas
                    .OrderBy(p => p.Estado)
                    .ThenBy(p => p.Cidade)
                    .ToListAsync();

                return Ok(pracas);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("test-single")]
        public async Task<IActionResult> TestarUtilizacaoUnica()
        {
            try
            {
                // Pega uma praça existente
                var praca = await _context.Pracas.FirstAsync();

                var utilizacaoTeste = new UtilizacaoDto
                {
                    DataUtilizacao = DateTime.Now.AddMinutes(-Random.Shared.Next(1, 60)),
                    PracaId = praca.Id,
                    Cidade = praca.Cidade,
                    Estado = praca.Estado,
                    ValorPago = (decimal)(Random.Shared.NextDouble() * 50 + 5), // Entre R$ 5 e R$ 55
                    TipoVeiculo = (TipoVeiculo)Random.Shared.Next(1, 4) // 1=Moto, 2=Carro, 3=Caminhão
                };

                // Simula chamada para o controller de utilização
                var utilizacaoService = HttpContext.RequestServices.GetRequiredService<IUtilizacaoService>();
                var resultado = await utilizacaoService.ProcessarUtilizacaoAsync(utilizacaoTeste);

                return Ok(new
                {
                    Message = "Utilização de teste criada",
                    UtilizacaoTeste = utilizacaoTeste,
                    Resultado = resultado
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("test-batch/{quantidade}")]
        public async Task<IActionResult> TestarLoteUtilizacoes(int quantidade = 10)
        {
            if (quantidade <= 0 || quantidade > 1000)
            {
                return BadRequest(new { Error = "Quantidade deve estar entre 1 e 1000" });
            }

            try
            {
                var pracas = await _context.Pracas.ToListAsync();
                if (!pracas.Any())
                {
                    return BadRequest(new { Error = "Nenhuma praça encontrada" });
                }

                var utilizacoes = new List<UtilizacaoDto>();
                var random = new Random();

                for (int i = 0; i < quantidade; i++)
                {
                    var praca = pracas[random.Next(pracas.Count)];

                    utilizacoes.Add(new UtilizacaoDto
                    {
                        DataUtilizacao = DateTime.Now.AddMinutes(-random.Next(1, 1440)), // Últimas 24h
                        PracaId = praca.Id,
                        Cidade = praca.Cidade,
                        Estado = praca.Estado,
                        ValorPago = (decimal)(random.NextDouble() * 50 + 5),
                        TipoVeiculo = (TipoVeiculo)random.Next(1, 4)
                    });
                }

                var lote = new UtilizacaoLoteDto { Utilizacoes = utilizacoes };

                var utilizacaoService = HttpContext.RequestServices.GetRequiredService<IUtilizacaoService>();
                var resultado = await utilizacaoService.ProcessarUtilizacoesLoteAsync(lote);

                return Ok(new
                {
                    Message = $"Lote de {quantidade} utilizações de teste criado",
                    Resultado = resultado
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("performance")]
        public async Task<IActionResult> TestePerformance()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Teste de consulta otimizada
                var utilizacoesHoje = await _context.Utilizacoes
                    .Where(u => u.DataUtilizacao >= DateTime.Today)
                    .CountAsync();

                var utilizacoesPorEstado = await _context.Utilizacoes
                    .Where(u => u.DataUtilizacao >= DateTime.Today.AddDays(-7))
                    .GroupBy(u => u.Estado)
                    .Select(g => new { Estado = g.Key, Total = g.Count() })
                    .ToListAsync();

                var mediaValor = await _context.Utilizacoes
                    .Where(u => u.DataUtilizacao >= DateTime.Today)
                    .AverageAsync(u => (double?)u.ValorPago) ?? 0;

                stopwatch.Stop();

                return Ok(new
                {
                    TempoConsulta = $"{stopwatch.ElapsedMilliseconds}ms",
                    UtilizacoesHoje = utilizacoesHoje,
                    UtilizacoesPorEstado = utilizacoesPorEstado,
                    MediaValorHoje = mediaValor,
                    Performance = stopwatch.ElapsedMilliseconds < 1000 ? "Excelente" :
                                 stopwatch.ElapsedMilliseconds < 5000 ? "Boa" : "Precisa otimização"
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BadRequest(new
                {
                    Error = ex.Message,
                    TempoAteErro = $"{stopwatch.ElapsedMilliseconds}ms"
                });
            }
        }

        [HttpDelete("clear-test-data")]
        public async Task<IActionResult> LimparDadosTeste()
        {
            try
            {
                // Remove utilizações de teste (últimas 24h)
                var dataLimite = DateTime.Now.AddDays(-1);
                var utilizacoesTeste = await _context.Utilizacoes
                    .Where(u => u.DataInsercao >= dataLimite)
                    .ToListAsync();

                if (utilizacoesTeste.Any())
                {
                    _context.Utilizacoes.RemoveRange(utilizacoesTeste);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    Message = $"Removidas {utilizacoesTeste.Count} utilizações de teste",
                    DataLimite = dataLimite
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}