using System.Data;
using Thunders.TechTest.ApiService.Models;
using Microsoft.Data.SqlClient;

namespace Thunders.TechTest.ApiService.Services
{
    public interface IBulkInsertService
    {
        Task<int> InserirUtilizacoesAsync(IEnumerable<UtilizacaoPedagio> utilizacoes);
    }

    public class BulkInsertService : IBulkInsertService
    {
        private readonly string _connectionString;
        private readonly ILogger<BulkInsertService> _logger;

        public BulkInsertService(IConfiguration configuration, ILogger<BulkInsertService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentException("Connection string não encontrada");
            _logger = logger;
        }

        public async Task<int> InserirUtilizacoesAsync(IEnumerable<UtilizacaoPedagio> utilizacoes)
        {
            var utilizacoesList = utilizacoes.ToList();
            if (!utilizacoesList.Any()) return 0;

            const int BATCH_SIZE = 10000;
            var totalInseridas = 0;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                for (int i = 0; i < utilizacoesList.Count; i += BATCH_SIZE)
                {
                    var batch = utilizacoesList.Skip(i).Take(BATCH_SIZE);
                    var dataTable = ConvertToDataTable(batch);

                    using var bulkCopy = new SqlBulkCopy(connection)
                    {
                        DestinationTableName = "Utilizacoes",
                        BatchSize = BATCH_SIZE,
                        BulkCopyTimeout = 300
                    };

                    ConfigureColumnMappings(bulkCopy);

                    await bulkCopy.WriteToServerAsync(dataTable);
                    totalInseridas += dataTable.Rows.Count;

                    _logger.LogInformation("Batch {BatchNumber} inserido: {Count} utilizações",
                        (i / BATCH_SIZE) + 1, dataTable.Rows.Count);
                }

                return totalInseridas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar bulk insert de utilizações");
                throw;
            }
        }

        private static DataTable ConvertToDataTable(IEnumerable<UtilizacaoPedagio> utilizacoes)
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
                    utilizacao.Cidade,
                    utilizacao.Estado,
                    utilizacao.ValorPago,
                    (int)utilizacao.TipoVeiculo,
                    utilizacao.DataInsercao
                );
            }

            return dataTable;
        }

        private static void ConfigureColumnMappings(SqlBulkCopy bulkCopy)
        {
            bulkCopy.ColumnMappings.Add("DataUtilizacao", "DataUtilizacao");
            bulkCopy.ColumnMappings.Add("PracaId", "PracaId");
            bulkCopy.ColumnMappings.Add("Cidade", "Cidade");
            bulkCopy.ColumnMappings.Add("Estado", "Estado");
            bulkCopy.ColumnMappings.Add("ValorPago", "ValorPago");
            bulkCopy.ColumnMappings.Add("TipoVeiculo", "TipoVeiculo");
            bulkCopy.ColumnMappings.Add("DataInsercao", "DataInsercao");
        }
    }
}
