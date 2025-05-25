using ApiService.DTOs.Relatorios;
using Thunders.TechTest.ApiService.Dtos.Paginacao;
using Thunders.TechTest.ApiService.Dtos.Relatorios.FaturamentoPorHora;
using Thunders.TechTest.ApiService.Dtos.Relatorios.TopPracas;
using Thunders.TechTest.ApiService.Dtos.Relatorios.VeiculosPorPraca;

namespace Thunders.TechTest.ApiService.Services.Relatorios
{
    public interface IRelatorioService
    {
        Task<RelatorioResponseWrapperDto<List<RelatorioFaturamentoPorHoraResponseDto>>>
        ObterFaturamentoPorHoraAsync(RelatorioFaturamentoPorHoraRequestDto request, CancellationToken cancellationToken = default);

        Task<RelatorioResponseWrapperDto<List<RelatorioTopPracasResponseDto>>>
            ObterTopPracasPorMesAsync(RelatorioTopPracasRequestDto request, CancellationToken cancellationToken = default);

        Task<RelatorioResponseWrapperDto<List<RelatorioVeiculosPorPracaResponseDto>>>
            ObterVeiculosPorPracaAsync(RelatorioVeiculosPorPracaRequestDto request, CancellationToken cancellationToken = default);

        // Novos métodos com paginação
        Task<RelatorioResponseWrapperDto<PagedResponseDto<RelatorioFaturamentoPorHoraResponseDto>>>
            ObterFaturamentoPorHoraPaginadoAsync(PagedRelatorioFaturamentoPorHoraRequestDto request, CancellationToken cancellationToken = default);

        Task<RelatorioResponseWrapperDto<PagedResponseDto<RelatorioTopPracasResponseDto>>>
            ObterTopPracasPorMesPaginadoAsync(PagedRelatorioTopPracasRequestDto request, CancellationToken cancellationToken = default);

        Task<RelatorioResponseWrapperDto<PagedResponseDto<RelatorioVeiculosPorPracaResponseDto>>>
            ObterVeiculosPorPracaPaginadoAsync(PagedRelatorioVeiculosPorPracaRequestDto request, CancellationToken cancellationToken = default);
    }
}

