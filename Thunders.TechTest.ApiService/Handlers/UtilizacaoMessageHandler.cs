using Microsoft.EntityFrameworkCore;
using Rebus.Handlers;
using System.Data;
using System.Data.SqlClient;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Dtos;
using Thunders.TechTest.ApiService.Messages;
using Thunders.TechTest.ApiService.Models;

namespace Thunders.TechTest.ApiService.Handlers
{
    public class UtilizacaoMessageHandler : IHandleMessages<UtilizacaoMessage>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<UtilizacaoMessageHandler> _logger;

        public UtilizacaoMessageHandler(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<UtilizacaoMessageHandler> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task Handle(UtilizacaoMessage message)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var processadas = 0;
            var comErro = 0;

            try
            {
                _logger.LogInformation("Processando lote de {Count} utilizações", message.Utilizacoes.Count);

                // Para lotes grandes, usa bulk insert
                if (message.Utilizacoes.Count > 100)
                {
                    await ProcessarComBulkInsert(context, message.Utilizacoes);
                    processadas = message.Utilizacoes.Count;
                }
                else
                {
                    // Para lotes pequenos, usa EF Core normal
                    var resultado = await ProcessarComEntityFramework(context, message.Utilizacoes);
                    processadas = resultado.Processadas;
                    comErro = resultado.ComErro;
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "Lote processado: {Processadas} utilizações em {ElapsedMs}ms, {ComErro} com erro",
                    processadas, stopwatch.ElapsedMilliseconds, comErro);

                // Registra métricas de telemetria
                //using var activity = System.Diagnostics.Activity.StartActivity("ProcessarUtilizacoes");
                //activity?.SetTag("utilizacoes.processadas", processadas);
                //activity?.SetTag("utilizacoes.erro", comErro);
                //activity?.SetTag("tempo.processamento", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Erro ao processar lote de utilizações");
                throw; // Rebus vai reprocessar a mensagem
            }
        }

        private async Task ProcessarComBulkInsert(AppDbContext context, List<UtilizacaoDto> utilizacoes)
        {
            var connectionString = context.Database.GetConnectionString();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = "Utilizacoes",
                BatchSize = 1000,
                BulkCopyTimeout = 300 // 5 minutos
            };

            // Mapear colunas
            bulkCopy.ColumnMappings.Add("DataUtilizacao", "DataUtilizacao");
            bulkCopy.ColumnMappings.Add("PracaId", "PracaId");
            bulkCopy.ColumnMappings.Add("Cidade", "Cidade");
            bulkCopy.ColumnMappings.Add("Estado", "Estado");
            bulkCopy.ColumnMappings.Add("ValorPago", "ValorPago");
            bulkCopy.ColumnMappings.Add("TipoVeiculo", "TipoVeiculo");
            bulkCopy.ColumnMappings.Add("DataInsercao", "DataInsercao");

            var dataTable = ConvertToDataTable(utilizacoes);
            await bulkCopy.WriteToServerAsync(dataTable);
        }

        private async Task<(int Processadas, int ComErro)> ProcessarComEntityFramework(
            AppDbContext context, List<UtilizacaoDto> utilizacoes)
        {
            var processadas = 0;
            var comErro = 0;

            var utilizacoesEntities = new List<UtilizacaoPedagio>();

            foreach (var dto in utilizacoes)
            {
                try
                {
                    var entity = new UtilizacaoPedagio
                    {
                        DataUtilizacao = dto.DataUtilizacao,
                        PracaId = dto.PracaId,
                        Cidade = dto.Cidade.Trim().ToUpper(),
                        Estado = dto.Estado.Trim().ToUpper(),
                        ValorPago = dto.ValorPago,
                        TipoVeiculo = dto.TipoVeiculo,
                        DataInsercao = DateTime.Now
                    };

                    utilizacoesEntities.Add(entity);
                    processadas++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao converter utilização DTO para entidade");
                    comErro++;
                }
            }

            if (utilizacoesEntities.Any())
            {
                context.Utilizacoes.AddRange(utilizacoesEntities);
                await context.SaveChangesAsync();
            }

            return (processadas, comErro);
        }

        private static DataTable ConvertToDataTable(List<UtilizacaoDto> utilizacoes)
        {
            var dataTable = new DataTable();

            // Definir colunas
            dataTable.Columns.Add("DataUtilizacao", typeof(DateTime));
            dataTable.Columns.Add("PracaId", typeof(int));
            dataTable.Columns.Add("Cidade", typeof(string));
            dataTable.Columns.Add("Estado", typeof(string));
            dataTable.Columns.Add("ValorPago", typeof(decimal));
            dataTable.Columns.Add("TipoVeiculo", typeof(int));
            dataTable.Columns.Add("DataInsercao", typeof(DateTime));

            // Adicionar linhas
            foreach (var utilizacao in utilizacoes)
            {
                dataTable.Rows.Add(
                    utilizacao.DataUtilizacao,
                    utilizacao.PracaId,
                    utilizacao.Cidade.Trim().ToUpper(),
                    utilizacao.Estado.Trim().ToUpper(),
                    utilizacao.ValorPago,
                    (int)utilizacao.TipoVeiculo,
                    DateTime.Now
                );
            }

            return dataTable;
        }
    }
}
