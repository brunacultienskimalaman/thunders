using ApiService.DTOs.Relatorios;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Dtos.Paginacao;
using Thunders.TechTest.ApiService.Dtos.Relatorios.FaturamentoPorHora;
using Thunders.TechTest.ApiService.Dtos.Relatorios.TopPracas;
using Thunders.TechTest.ApiService.Dtos.Relatorios.VeiculosPorPraca;
using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Models.Enums;
using Thunders.TechTest.ApiService.Services.Relatorios;

namespace ApiService.Services;

public class RelatorioService : IRelatorioService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RelatorioService> _logger;

    public RelatorioService(AppDbContext context, ILogger<RelatorioService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ADICIONAR NO RelatorioService.cs - Método de Faturamento com Paginação
    public async Task<RelatorioResponseWrapperDto<PagedResponseDto<RelatorioFaturamentoPorHoraResponseDto>>>
        ObterFaturamentoPorHoraPaginadoAsync(PagedRelatorioFaturamentoPorHoraRequestDto request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var idSolicitacao = Guid.NewGuid();

        try
        {
            _logger.LogInformation("Iniciando relatório de faturamento por hora PAGINADO. ID: {IdSolicitacao}, Página: {Page}, Tamanho: {PageSize}",
                idSolicitacao, request.Page, request.PageSize);

            var query = _context.Utilizacoes
                .Where(u => u.DataUtilizacao >= request.DataInicio && u.DataUtilizacao <= request.DataFim);

            // Filtro opcional por cidade
            if (!string.IsNullOrWhiteSpace(request.Cidade))
            {
                query = query.Where(u => u.Cidade.ToLower().Contains(request.Cidade.ToLower()));
            }

            // Query agrupada (mesmo do método original)
            var groupedQuery = query
                .GroupBy(u => new
                {
                    u.Cidade,
                    u.Estado,
                    Ano = u.DataUtilizacao.Year,
                    Mes = u.DataUtilizacao.Month,
                    Dia = u.DataUtilizacao.Day,
                    Hora = u.DataUtilizacao.Hour
                })
                .Select(g => new
                {
                    g.Key.Cidade,
                    g.Key.Estado,
                    g.Key.Ano,
                    g.Key.Mes,
                    g.Key.Dia,
                    g.Key.Hora,
                    ValorTotal = g.Sum(u => u.ValorPago),
                    TotalPassagens = g.Count()
                })
                .OrderBy(r => r.Cidade)
                .ThenBy(r => r.Ano)
                .ThenBy(r => r.Mes)
                .ThenBy(r => r.Dia)
                .ThenBy(r => r.Hora);

            // Contar total de registros (ANTES da paginação)
            var totalRecords = await groupedQuery.CountAsync(cancellationToken);

            // Aplicar paginação
            var skip = (request.Page - 1) * request.PageSize;
            var pagedData = await groupedQuery
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Converter para DTO final
            var resultadoFinal = pagedData
                .Select(r => new RelatorioFaturamentoPorHoraResponseDto
                {
                    Cidade = r.Cidade,
                    Estado = r.Estado,
                    DataHora = new DateTime(r.Ano, r.Mes, r.Dia, r.Hora, 0, 0),
                    ValorTotal = r.ValorTotal,
                    TotalPassagens = r.TotalPassagens
                })
                .ToList();

            // Criar resposta paginada
            var pagedResponse = PagedResponseDto<RelatorioFaturamentoPorHoraResponseDto>
                .Create(resultadoFinal, request.Page, request.PageSize, totalRecords);

            stopwatch.Stop();

            await SalvarHistoricoRelatorioAsync("FaturamentoPorHoraPaginado", request, pagedResponse,
                stopwatch.ElapsedMilliseconds, idSolicitacao, StatusProcessamento.Concluido);

            _logger.LogInformation("Relatório de faturamento PAGINADO concluído. ID: {IdSolicitacao}, Tempo: {Tempo}ms, Página: {Page}/{TotalPages}, Registros: {Registros}/{Total}",
                idSolicitacao, stopwatch.ElapsedMilliseconds, pagedResponse.Page, pagedResponse.TotalPages, pagedResponse.Data.Count, pagedResponse.TotalRecords);

            return RelatorioResponseWrapperDto<PagedResponseDto<RelatorioFaturamentoPorHoraResponseDto>>
                .ComSucesso(pagedResponse, (int)stopwatch.ElapsedMilliseconds, pagedResponse.TotalRecords);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Relatório de faturamento PAGINADO cancelado por timeout. ID: {IdSolicitacao}", idSolicitacao);

            await SalvarHistoricoRelatorioAsync("FaturamentoPorHoraPaginado", request, null,
                stopwatch.ElapsedMilliseconds, idSolicitacao, StatusProcessamento.Cancelado, "Timeout");

            return RelatorioResponseWrapperDto<PagedResponseDto<RelatorioFaturamentoPorHoraResponseDto>>
                .ComErro("Relatório cancelado por timeout", (int)stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao processar relatório de faturamento PAGINADO. ID: {IdSolicitacao}", idSolicitacao);

            await SalvarHistoricoRelatorioAsync("FaturamentoPorHoraPaginado", request, null,
                stopwatch.ElapsedMilliseconds, idSolicitacao, StatusProcessamento.Erro, ex.Message);

            return RelatorioResponseWrapperDto<PagedResponseDto<RelatorioFaturamentoPorHoraResponseDto>>
                .ComErro($"Erro interno: {ex.Message}", (int)stopwatch.ElapsedMilliseconds);
        }
    }

    // ADICIONAR NO RelatorioService.cs - Método de Top Praças com Paginação
    public async Task<RelatorioResponseWrapperDto<PagedResponseDto<RelatorioTopPracasResponseDto>>>
        ObterTopPracasPorMesPaginadoAsync(PagedRelatorioTopPracasRequestDto request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var idSolicitacao = Guid.NewGuid();

        try
        {
            _logger.LogInformation("Iniciando relatório de top praças PAGINADO. ID: {IdSolicitacao}, Página: {Page}, Tamanho: {PageSize}",
                idSolicitacao, request.Page, request.PageSize);

            var dataInicio = new DateTime(request.Ano, request.Mes, 1);
            var dataFim = dataInicio.AddMonths(1).AddDays(-1);

            var groupedQuery = _context.Utilizacoes
                .Include(u => u.Praca)
                .Where(u => u.DataUtilizacao >= dataInicio && u.DataUtilizacao <= dataFim)
                .GroupBy(u => new
                {
                    u.PracaId,
                    NomePraca = u.Praca!.Nome,
                    u.Praca.Cidade,
                    u.Praca.Estado
                })
                .Select(g => new
                {
                    g.Key.PracaId,
                    g.Key.NomePraca,
                    g.Key.Cidade,
                    g.Key.Estado,
                    FaturamentoTotal = g.Sum(u => u.ValorPago),
                    TotalUtilizacoes = g.Count()
                })
                .OrderByDescending(r => r.FaturamentoTotal);

            // Contar total de registros
            var totalRecords = await groupedQuery.CountAsync(cancellationToken);

            // Aplicar paginação
            var skip = (request.Page - 1) * request.PageSize;
            var pagedData = await groupedQuery
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Converter para DTO com ranking baseado na posição global
            var resultadoFinal = pagedData
                .Select((item, index) => new RelatorioTopPracasResponseDto
                {
                    PracaId = item.PracaId,
                    NomePraca = item.NomePraca,
                    Cidade = item.Cidade,
                    Estado = item.Estado,
                    FaturamentoTotal = item.FaturamentoTotal,
                    TotalUtilizacoes = item.TotalUtilizacoes,
                    Ranking = skip + index + 1 // Ranking global considerando paginação
                })
                .ToList();

            // Criar resposta paginada
            var pagedResponse = PagedResponseDto<RelatorioTopPracasResponseDto>
                .Create(resultadoFinal, request.Page, request.PageSize, totalRecords);

            stopwatch.Stop();

            await SalvarHistoricoRelatorioAsync("TopPracasPorMesPaginado", request, pagedResponse,
                stopwatch.ElapsedMilliseconds, idSolicitacao, StatusProcessamento.Concluido);

            _logger.LogInformation("Relatório de top praças PAGINADO concluído. ID: {IdSolicitacao}, Tempo: {Tempo}ms, Página: {Page}/{TotalPages}, Registros: {Registros}/{Total}",
                idSolicitacao, stopwatch.ElapsedMilliseconds, pagedResponse.Page, pagedResponse.TotalPages, pagedResponse.Data.Count, pagedResponse.TotalRecords);

            return RelatorioResponseWrapperDto<PagedResponseDto<RelatorioTopPracasResponseDto>>
                .ComSucesso(pagedResponse, (int)stopwatch.ElapsedMilliseconds, pagedResponse.TotalRecords);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Relatório de top praças PAGINADO cancelado por timeout. ID: {IdSolicitacao}", idSolicitacao);

            await SalvarHistoricoRelatorioAsync("TopPracasPorMesPaginado", request, null,
                stopwatch.ElapsedMilliseconds, idSolicitacao, StatusProcessamento.Cancelado, "Timeout");

            return RelatorioResponseWrapperDto<PagedResponseDto<RelatorioTopPracasResponseDto>>
                .ComErro("Relatório cancelado por timeout", (int)stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao processar relatório de top praças PAGINADO. ID: {IdSolicitacao}", idSolicitacao);

            await SalvarHistoricoRelatorioAsync("TopPracasPorMesPaginado", request, null,
                stopwatch.ElapsedMilliseconds, idSolicitacao, StatusProcessamento.Erro, ex.Message);

            return RelatorioResponseWrapperDto<PagedResponseDto<RelatorioTopPracasResponseDto>>
                .ComErro($"Erro interno: {ex.Message}", (int)stopwatch.ElapsedMilliseconds);
        }
    }

    // ADICIONAR NO RelatorioService.cs - Método de Veículos por Praça com Paginação
    public async Task<RelatorioResponseWrapperDto<PagedResponseDto<RelatorioVeiculosPorPracaResponseDto>>>
        ObterVeiculosPorPracaPaginadoAsync(PagedRelatorioVeiculosPorPracaRequestDto request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var idSolicitacao = Guid.NewGuid();

        try
        {
            _logger.LogInformation("Iniciando relatório de veículos por praça PAGINADO. ID: {IdSolicitacao}, Página: {Page}, Tamanho: {PageSize}",
                idSolicitacao, request.Page, request.PageSize);

            var query = _context.Utilizacoes
                .Include(u => u.Praca)
                .Where(u => u.DataUtilizacao >= request.DataInicio && u.DataUtilizacao <= request.DataFim);

            // Filtro opcional por praça
            if (request.PracaId.HasValue)
            {
                query = query.Where(u => u.PracaId == request.PracaId.Value);
            }

            // Agrupar por praça primeiro
            var groupedQuery = query
                .GroupBy(u => new
                {
                    u.PracaId,
                    NomePraca = u.Praca!.Nome,
                    u.Praca.Cidade,
                    u.Praca.Estado
                })
                .OrderBy(g => g.Key.Cidade)
                .ThenBy(g => g.Key.NomePraca);

            // Contar total de praças únicas
            var totalRecords = await groupedQuery.CountAsync(cancellationToken);

            // Aplicar paginação nas praças
            var skip = (request.Page - 1) * request.PageSize;
            var pagedPracaGroups = await groupedQuery
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var resultado = new List<RelatorioVeiculosPorPracaResponseDto>();

            // Processar cada grupo de praça (já paginado)
            foreach (var pracaGroup in pagedPracaGroups)
            {
                var totalUtilizacoes = pracaGroup.Count();
                var faturamentoTotal = pracaGroup.Sum(u => u.ValorPago);

                var tiposVeiculos = pracaGroup
                    .GroupBy(u => u.TipoVeiculo)
                    .Select(g => new TipoVeiculoResumoDto
                    {
                        TipoVeiculo = g.Key,
                        DescricaoTipo = ObterDescricaoTipoVeiculo(g.Key),
                        Quantidade = g.Count(),
                        ValorTotal = g.Sum(u => u.ValorPago),
                        PercentualQuantidade = Math.Round((decimal)g.Count() / totalUtilizacoes * 100, 2)
                    })
                    .OrderBy(t => t.TipoVeiculo)
                    .ToList();

                resultado.Add(new RelatorioVeiculosPorPracaResponseDto
                {
                    PracaId = pracaGroup.Key.PracaId,
                    NomePraca = pracaGroup.Key.NomePraca,
                    Cidade = pracaGroup.Key.Cidade,
                    Estado = pracaGroup.Key.Estado,
                    TiposVeiculos = tiposVeiculos,
                    TotalUtilizacoes = totalUtilizacoes,
                    FaturamentoTotal = faturamentoTotal
                });
            }

            // Criar resposta paginada
            var pagedResponse = PagedResponseDto<RelatorioVeiculosPorPracaResponseDto>
                .Create(resultado, request.Page, request.PageSize, totalRecords);

            stopwatch.Stop();

            await SalvarHistoricoRelatorioAsync("VeiculosPorPracaPaginado", request, pagedResponse,
                stopwatch.ElapsedMilliseconds, idSolicitacao, StatusProcessamento.Concluido);

            _logger.LogInformation("Relatório de veículos PAGINADO concluído. ID: {IdSolicitacao}, Tempo: {Tempo}ms, Página: {Page}/{TotalPages}, Registros: {Registros}/{Total}",
                idSolicitacao, stopwatch.ElapsedMilliseconds, pagedResponse.Page, pagedResponse.TotalPages, pagedResponse.Data.Count, pagedResponse.TotalRecords);

            return RelatorioResponseWrapperDto<PagedResponseDto<RelatorioVeiculosPorPracaResponseDto>>
                .ComSucesso(pagedResponse, (int)stopwatch.ElapsedMilliseconds, pagedResponse.TotalRecords);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Relatório de veículos PAGINADO cancelado por timeout. ID: {IdSolicitacao}", idSolicitacao);

            await SalvarHistoricoRelatorioAsync("VeiculosPorPracaPaginado", request, null,
                stopwatch.ElapsedMilliseconds, idSolicitacao, StatusProcessamento.Cancelado, "Timeout");

            return RelatorioResponseWrapperDto<PagedResponseDto<RelatorioVeiculosPorPracaResponseDto>>
                .ComErro("Relatório cancelado por timeout", (int)stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao processar relatório de veículos PAGINADO. ID: {IdSolicitacao}", idSolicitacao);

            await SalvarHistoricoRelatorioAsync("VeiculosPorPracaPaginado", request, null,
                stopwatch.ElapsedMilliseconds, idSolicitacao, StatusProcessamento.Erro, ex.Message);

            return RelatorioResponseWrapperDto<PagedResponseDto<RelatorioVeiculosPorPracaResponseDto>>
                .ComErro($"Erro interno: {ex.Message}", (int)stopwatch.ElapsedMilliseconds);
        }
    }
    private static string ObterDescricaoTipoVeiculo(TipoVeiculo tipo)
    {
        return tipo switch
        {
            TipoVeiculo.Moto => "Motocicleta",
            TipoVeiculo.Carro => "Automóvel",
            TipoVeiculo.Caminhao => "Caminhão",
            _ => "Não identificado"
        };
    }
    private async Task SalvarHistoricoRelatorioAsync<TRequest>(
        string tipoRelatorio,
        TRequest request,
        object? response,
        long tempoProcessamento,
        Guid idSolicitacao,
        StatusProcessamento status,
        string? mensagemErro = null)
    {
        try
        {
            var relatorio = new RelatorioProcessado
            {
                TipoRelatorio = tipoRelatorio,
                ParametrosJson = System.Text.Json.JsonSerializer.Serialize(request),
                ResultadoJson = response != null ? System.Text.Json.JsonSerializer.Serialize(response) : null,
                DataProcessamento = DateTime.Now,
                TempoProcessamento = (int)tempoProcessamento,
                IdSolicitacao = idSolicitacao,
                Status = status,
                MensagemErro = mensagemErro
            };

            _context.Relatorios.Add(relatorio);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar histórico do relatório {TipoRelatorio} - ID: {IdSolicitacao}",
                tipoRelatorio, idSolicitacao);
        }
    }

} 