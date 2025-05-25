using Thunders.TechTest.ApiService.Models.Enums;

namespace Thunders.TechTest.ApiService.Dtos.Relatorios.VeiculosPorPraca
{
    public class TipoVeiculoResumoDto
    {
        public TipoVeiculo TipoVeiculo { get; set; }
        public string DescricaoTipo { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal PercentualQuantidade { get; set; }
    }
}
